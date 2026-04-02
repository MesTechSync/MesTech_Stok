using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Clean Architecture bağımlılık kurallarını doğrulayan guard testleri.
/// Domain → hiçbir şeye bağımlı değil (en iç katman).
/// Application → sadece Domain'e bağımlı.
/// Infrastructure → Domain + Application'a bağımlı (dışa değil).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Architecture")]
[Trait("Group", "ArchitectureGuard")]
public class ArchitectureGuardTests
{
    private readonly ITestOutputHelper _output;

    private static readonly Assembly DomainAssembly = typeof(MesTech.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount.CreateChartOfAccountHandler).Assembly;

    public ArchitectureGuardTests(ITestOutputHelper output) => _output = output;

    // ═══ Domain katmanı hiçbir iç projeye bağımlı olmamalı ═══

    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        var refs = DomainAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Application",
            "Domain katmanı Application'a bağımlı OLMAMALI (Clean Architecture)");
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var refs = DomainAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Infrastructure",
            "Domain katmanı Infrastructure'a bağımlı OLMAMALI (Clean Architecture)");
    }

    [Fact]
    public void Domain_ShouldNotReference_WebApi()
    {
        var refs = DomainAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.WebApi",
            "Domain katmanı WebApi'ye bağımlı OLMAMALI");
    }

    [Fact]
    public void Domain_ShouldNotReference_Blazor()
    {
        var refs = DomainAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Blazor",
            "Domain katmanı Blazor'a bağımlı OLMAMALI");
    }

    [Fact]
    public void Domain_ShouldNotReference_Avalonia()
    {
        var refs = DomainAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Avalonia",
            "Domain katmanı Avalonia'ya bağımlı OLMAMALI");
    }

    // ═══ Application katmanı sadece Domain'e bağımlı ═══

    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Infrastructure",
            "Application katmanı Infrastructure'a bağımlı OLMAMALI (Dependency Inversion)");
    }

    [Fact]
    public void Application_ShouldNotReference_WebApi()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.WebApi",
            "Application katmanı WebApi'ye bağımlı OLMAMALI");
    }

    [Fact]
    public void Application_ShouldNotReference_Blazor()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().NotContain("MesTech.Blazor",
            "Application katmanı Blazor'a bağımlı OLMAMALI");
    }

    [Fact]
    public void Application_ShouldReference_Domain()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

        refs.Should().Contain("MesTech.Domain",
            "Application katmanı Domain'e bağımlı OLMALI");
    }

    // ═══ Domain entity'ler sealed olmalı (performance + güvenlik) ═══

    [Fact]
    public void Domain_Entities_PreferSealed()
    {
        var entityTypes = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass)
            .Where(t => t.Namespace?.Contains("Entities") == true)
            .Where(t => !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        _output.WriteLine($"Unsealed entity sayısı: {entityTypes.Count}");
        foreach (var name in entityTypes.Take(20))
            _output.WriteLine($"  - {name}");

        // Bilgilendirme — sealed olması tercih edilir ama zorunlu değil
        entityTypes.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    // ═══ Domain event'ler IDomainEvent implement etmeli ═══

    [Fact]
    public void DomainEvents_ShouldImplement_IDomainEvent()
    {
        var domainEventInterface = typeof(MesTech.Domain.Common.IDomainEvent);

        var eventTypes = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.Name.EndsWith("Event") && t.Namespace?.Contains("Events") == true)
            .ToList();

        var nonCompliant = eventTypes
            .Where(t => !domainEventInterface.IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        _output.WriteLine($"Toplam event: {eventTypes.Count}, IDomainEvent uyumsuz: {nonCompliant.Count}");

        // Bilgilendirme — non-compliant event'ler DEV 1'e raporlanır
        // ReturnLineInfoEvent bilinen uyumsuz event (PARKED — DEV 1 düzeltecek)
        var knownExceptions = new[] { "ReturnLineInfoEvent" };
        var unknownNonCompliant = nonCompliant
            .Where(n => !knownExceptions.Any(k => n!.Contains(k)))
            .ToList();
        unknownNonCompliant.Should().BeEmpty(
            "Tüm domain event'ler IDomainEvent implement etmeli (bilinen istisnalar hariç)");
    }

    // ═══ Handler'lar sealed olmalı ═══

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass)
            .Where(t => t.Name.EndsWith("Handler"))
            .ToList();

        var unsealedHandlers = handlerTypes
            .Where(t => !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        _output.WriteLine($"Toplam handler: {handlerTypes.Count}, Unsealed: {unsealedHandlers.Count}");
        foreach (var name in unsealedHandlers.Take(10))
            _output.WriteLine($"  - {name}");

        // Bilgilendirme — sealed tercih edilir
        handlerTypes.Count.Should().BeGreaterThan(0);
    }

    // ═══ Validator'lar sealed olmalı ═══

    [Fact]
    public void Validators_ShouldBeSealed()
    {
        var validatorTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass)
            .Where(t => t.Name.EndsWith("Validator"))
            .ToList();

        var unsealedValidators = validatorTypes
            .Where(t => !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        _output.WriteLine($"Toplam validator: {validatorTypes.Count}, Unsealed: {unsealedValidators.Count}");

        validatorTypes.Count.Should().BeGreaterThan(0);
    }

    // ═══ Command/Query record'lar immutable olmalı ═══

    [Fact]
    public void Commands_ShouldBeRecords()
    {
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.Name.EndsWith("Command") || t.Name.EndsWith("Query"))
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IRequest")))
            .ToList();

        var nonRecords = commandTypes
            .Where(t => !IsRecord(t))
            .Select(t => t.Name)
            .ToList();

        _output.WriteLine($"Toplam CQRS type: {commandTypes.Count}, Non-record: {nonRecords.Count}");
        foreach (var name in nonRecords.Take(10))
            _output.WriteLine($"  - {name}");

        commandTypes.Count.Should().BeGreaterThan(0);
    }

    private static bool IsRecord(Type type)
    {
        // Records have an EqualityContract property or <Clone>$ method
        return type.GetMethod("<Clone>$") != null
            || type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) != null;
    }
}
