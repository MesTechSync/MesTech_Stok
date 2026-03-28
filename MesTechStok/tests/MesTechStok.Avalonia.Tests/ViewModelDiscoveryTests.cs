using System.Reflection;
using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 12: Reflection-based ViewModel discovery tests
// Scans ALL ViewModelBase subclasses in the assembly,
// creates each via mock-injected constructor, and validates:
//   1. Constructor doesn't throw (DI wiring check)
//   2. IsLoading defaults to false
//   3. HasError defaults to false
//   4. Dispose doesn't throw
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ViewModelDiscoveryTests
{
    private static readonly Assembly AvaloniaAssembly = typeof(ViewModelBase).Assembly;

    /// <summary>
    /// Find all concrete ViewModelBase subclasses in the Avalonia assembly.
    /// </summary>
    public static IEnumerable<object[]> AllViewModelTypes()
    {
        var vmTypes = AvaloniaAssembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsSubclassOf(typeof(ViewModelBase))
                        && t != typeof(ViewModelBase))
            .OrderBy(t => t.Name);

        foreach (var type in vmTypes)
            yield return [type];
    }

    /// <summary>
    /// Create a mock for any interface/abstract type using Moq via reflection.
    /// Returns null for types that cannot be mocked.
    /// </summary>
    private static object? CreateMock(Type paramType)
    {
        if (!paramType.IsInterface && !paramType.IsAbstract)
            return null;

        try
        {
            var mockType = typeof(Mock<>).MakeGenericType(paramType);
            var mock = Activator.CreateInstance(mockType);
            var objectProp = mockType.GetProperty("Object");
            return objectProp?.GetValue(mock);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Try to instantiate a ViewModel by finding a constructor and injecting mocks.
    /// Returns null if no suitable constructor can be satisfied.
    /// </summary>
    private static ViewModelBase? TryCreateViewModel(Type vmType)
    {
        // Try parameterless constructor first
        var parameterlessCtor = vmType.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor is not null)
        {
            return (ViewModelBase)parameterlessCtor.Invoke([]);
        }

        // Find the constructor with the most parameters (typically the DI constructor)
        var constructors = vmType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            var allResolved = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                if (paramType.IsInterface || paramType.IsAbstract)
                {
                    args[i] = CreateMock(paramType);
                    if (args[i] is null)
                    {
                        // Try nullable — pass null for optional params
                        if (parameters[i].HasDefaultValue || paramType.IsClass)
                            args[i] = null;
                        else
                        {
                            allResolved = false;
                            break;
                        }
                    }
                }
                else if (paramType == typeof(string))
                {
                    args[i] = string.Empty;
                }
                else if (paramType.IsValueType)
                {
                    args[i] = Activator.CreateInstance(paramType);
                }
                else
                {
                    // Try to create a mock for concrete class
                    args[i] = CreateMock(paramType) ?? (parameters[i].HasDefaultValue ? null : Activator.CreateInstance(paramType));
                }
            }

            if (!allResolved) continue;

            try
            {
                return (ViewModelBase)ctor.Invoke(args);
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    [Theory]
    [MemberData(nameof(AllViewModelTypes))]
    public void ViewModel_ShouldBeInstantiable(Type vmType)
    {
        // Act
        var vm = TryCreateViewModel(vmType);

        // Assert — constructor should succeed
        vm.Should().NotBeNull(
            $"{vmType.Name} should be instantiable with mock dependencies. " +
            $"Constructors: {string.Join(", ", vmType.GetConstructors().Select(c => $"({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})"))}");
    }

    [Theory]
    [MemberData(nameof(AllViewModelTypes))]
    public void ViewModel_ShouldDefaultToNotLoading(Type vmType)
    {
        var vm = TryCreateViewModel(vmType);
        if (vm is null) return; // Skip if can't instantiate

        vm.IsLoading.Should().BeFalse($"{vmType.Name}.IsLoading should default to false");
    }

    [Theory]
    [MemberData(nameof(AllViewModelTypes))]
    public void ViewModel_ShouldDefaultToNoError(Type vmType)
    {
        var vm = TryCreateViewModel(vmType);
        if (vm is null) return;

        vm.HasError.Should().BeFalse($"{vmType.Name}.HasError should default to false");
    }

    [Theory]
    [MemberData(nameof(AllViewModelTypes))]
    public void ViewModel_ShouldDisposeWithoutThrowing(Type vmType)
    {
        var vm = TryCreateViewModel(vmType);
        if (vm is null) return;

        var act = () => vm.Dispose();
        act.Should().NotThrow($"{vmType.Name}.Dispose() should not throw");
    }

    [Fact]
    public void Assembly_ShouldContainAtLeast100ViewModels()
    {
        var count = AllViewModelTypes().Count();
        count.Should().BeGreaterThanOrEqualTo(100,
            "Avalonia assembly should contain at least 100 concrete ViewModels");
    }
}
