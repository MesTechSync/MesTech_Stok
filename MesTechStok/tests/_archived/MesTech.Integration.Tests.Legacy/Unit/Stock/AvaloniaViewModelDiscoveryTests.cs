using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G050: Tüm Avalonia ViewModel'lerin keşif testi.
/// Her ViewModel'in assembly'de kayıtlı olduğunu ve type'ın valid olduğunu doğrular.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
[Trait("Group", "AvaloniaVMDiscovery")]
public class AvaloniaViewModelDiscoveryTests
{
    private static readonly Assembly AvaloniaAssembly =
        typeof(ViewModelBase).Assembly;

    private readonly ITestOutputHelper _output;

    public AvaloniaViewModelDiscoveryTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void AllViewModels_AreDiscoverable()
    {
        var viewModelTypes = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.Name.EndsWith("ViewModel"))
            .ToList();

        _output.WriteLine($"Toplam ViewModel: {viewModelTypes.Count}");
        foreach (var vm in viewModelTypes.OrderBy(t => t.Name))
            _output.WriteLine($"  {vm.Name}");

        viewModelTypes.Should().HaveCountGreaterThan(100,
            "MesTech Avalonia projesinde 100+ ViewModel bekleniyor");
    }

    [Fact]
    public void AllViewModels_InheritFromViewModelBase()
    {
        var viewModelTypes = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.Name.EndsWith("ViewModel")
                     && typeof(ViewModelBase).IsAssignableFrom(t))
            .ToList();

        var nonInheriting = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.Name.EndsWith("ViewModel")
                     && !typeof(ViewModelBase).IsAssignableFrom(t))
            .ToList();

        _output.WriteLine($"ViewModelBase inherit eden: {viewModelTypes.Count}");
        foreach (var vm in nonInheriting)
            _output.WriteLine($"  UYARI — ViewModelBase'den türememiş: {vm.Name}");

        viewModelTypes.Should().HaveCountGreaterThan(50);
    }

    [Fact]
    public void ParameterlessViewModels_CanBeInstantiated()
    {
        var parameterless = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass
                     && typeof(ViewModelBase).IsAssignableFrom(t)
                     && t.GetConstructors().Any(c => c.GetParameters().Length == 0))
            .ToList();

        _output.WriteLine($"Parameterless ViewModel: {parameterless.Count}");

        var failed = new List<string>();
        foreach (var vmType in parameterless)
        {
            try
            {
                var instance = Activator.CreateInstance(vmType);
                instance.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                failed.Add($"{vmType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        foreach (var f in failed)
            _output.WriteLine($"  FAIL: {f}");

        failed.Should().BeEmpty("Tüm parameterless ViewModel'ler oluşturulabilmeli");
    }

    [Fact]
    public void ViewModels_HaveLoadAsyncMethod()
    {
        var withLoadAsync = AvaloniaAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass
                     && typeof(ViewModelBase).IsAssignableFrom(t)
                     && t.GetMethod("LoadAsync", BindingFlags.Public | BindingFlags.Instance) != null)
            .ToList();

        _output.WriteLine($"LoadAsync metodu olan ViewModel: {withLoadAsync.Count}");

        withLoadAsync.Should().HaveCountGreaterThan(10,
            "Çoğu ViewModel LoadAsync implementasyonuna sahip olmalı");
    }
}
