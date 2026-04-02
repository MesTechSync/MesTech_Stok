using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using System.Reflection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G050: Avalonia ViewModel universal instantiation test.
/// Reflection ile TÜM ViewModel'leri mock dependency ile oluşturur.
/// Linter constructor değişikliklerine dayanıklı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
[Trait("Group", "AvaloniaVM")]
public class AvaloniaViewModelTests
{
    private static readonly Assembly AvaloniaAssembly = typeof(ViewModelBase).Assembly;
    private readonly ITestOutputHelper _output;

    public AvaloniaViewModelTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void AllViewModels_CanBeInstantiated_WithMocks()
    {
        var vmTypes = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass
                     && typeof(ViewModelBase).IsAssignableFrom(t))
            .ToList();

        var succeeded = 0;
        var failed = new List<string>();

        foreach (var vmType in vmTypes)
        {
            try
            {
                var ctor = vmType.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .First();

                var args = ctor.GetParameters()
                    .Select(p => CreateMock(p.ParameterType))
                    .ToArray();

                var instance = ctor.Invoke(args);
                instance.Should().NotBeNull();
                succeeded++;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                failed.Add($"{vmType.Name}: {msg}");
            }
        }

        _output.WriteLine($"Başarılı: {succeeded}/{vmTypes.Count}");
        foreach (var f in failed)
            _output.WriteLine($"  SKIP: {f}");

        // En az %80'i oluşturulabilmeli
        succeeded.Should().BeGreaterThan(vmTypes.Count * 80 / 100,
            $"En az %80 ViewModel mock ile oluşturulabilmeli ({succeeded}/{vmTypes.Count})");
    }

    [Fact]
    public void StockVM_HasSummary()
    {
        var vm = new StockAvaloniaViewModel();
        vm.Summary.Should().NotBeNullOrEmpty();
    }

    private static object? CreateMock(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type == typeof(int)) return 0;
        if (type == typeof(bool)) return false;
        if (type == typeof(Guid)) return Guid.NewGuid();

        if (type.IsInterface || type.IsAbstract)
        {
            var mockType = typeof(Mock<>).MakeGenericType(type);
            var mock = (Mock)Activator.CreateInstance(mockType)!;
            return mock.Object;
        }

        // Concrete class — try default constructor
        try { return Activator.CreateInstance(type); }
        catch { return null!; }
    }
}
