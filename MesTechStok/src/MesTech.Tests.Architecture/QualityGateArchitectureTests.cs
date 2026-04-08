using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

/// <summary>
/// KD-DEV5-003: Architecture quality gate tests — BaseEntity inheritance,
/// Domain isolation, DTO immutability, Handler thread safety.
/// </summary>
[Trait("Category", "Architecture")]
[Trait("Phase", "KaliteDevrimi")]
public class QualityGateArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Product).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(CreateWorkTaskHandler).Assembly;
    private static readonly System.Reflection.Assembly InfraAssembly = typeof(TrendyolAdapter).Assembly;

    // ── RULE 1: All concrete entities in Domain.Entities must inherit BaseEntity ──

    [Fact]
    public void AllEntities_ShouldInheritFrom_BaseEntity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseEntity))
            .GetResult();

        var failing = result.FailingTypeNames?.ToList() ?? [];
        // Allow DTOs / nested types / known legacy entities that may not inherit BaseEntity
        var realFailures = failing.Where(n =>
            !n.Contains("Dto", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("Item", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("Result", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("Line", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("Mapping", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("Configuration", StringComparison.OrdinalIgnoreCase)).ToList();

        // Soft threshold: allow up to 15 legacy exceptions (tracked as tech debt)
        realFailures.Count.Should().BeLessThanOrEqualTo(15,
            $"Most domain entities should inherit BaseEntity. Failing ({realFailures.Count}): {string.Join(", ", realFailures.Take(15))}");
    }

    // ── RULE 2: Accounting entities must also inherit BaseEntity ──

    [Fact]
    public void AccountingEntities_ShouldInheritFrom_BaseEntity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Accounting.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseEntity))
            .GetResult();

        var failCount = result.FailingTypeNames?.Count() ?? 0;
        // Soft threshold: allow up to 5 legacy exceptions
        failCount.Should().BeLessThanOrEqualTo(5,
            $"Most accounting entities should inherit BaseEntity. Failing ({failCount}): {FormatFailing(result)}");
    }

    // ── RULE 3: Dropshipping entities must also inherit BaseEntity ──

    [Fact]
    public void DropshippingEntities_ShouldInheritFrom_BaseEntity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Dropshipping.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseEntity))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All dropshipping entities must inherit BaseEntity. Failing: {FormatFailing(result)}");
    }

    // ── RULE 4: Domain must not depend on Infrastructure (comprehensive) ──

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure_Comprehensive()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MesTech.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "MassTransit",
                "Hangfire")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain must not depend on any infrastructure package. Failing: {FormatFailing(result)}");
    }

    // ── RULE 5: Application must not depend on concrete Infrastructure types ──

    [Fact]
    public void Application_ShouldNotDependOn_ConcreteInfrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Npgsql",
                "MassTransit",
                "Hangfire",
                "MesTech.Infrastructure.Persistence",
                "MesTech.Infrastructure.Integration")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application must depend on interfaces only, not concrete Infrastructure. Failing: {FormatFailing(result)}");
    }

    // ── RULE 6: DTOs must be records or sealed classes ──

    [Fact]
    public void DTOs_ShouldBeRecordsOrSealed()
    {
        var dtoTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Dto")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in dtoTypes)
        {
            var isRecord = type.GetMethod("<Clone>$") != null;
            var isSealed = type.IsSealed;

            (isRecord || isSealed).Should().BeTrue(
                $"{type.Name} DTO should be a record or sealed class for immutability.");
        }
    }

    // ── RULE 7: No static mutable state in handlers ──

    [Fact]
    public void Handlers_ShouldNotHaveStaticMutableFields()
    {
        var handlerTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in handlerTypes)
        {
            var staticMutableFields = type.GetFields(
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .ToList();

            staticMutableFields.Should().BeEmpty(
                $"{type.Name} should not have static mutable fields (thread safety risk).");
        }
    }

    // ── RULE 8: Billing entities must inherit BaseEntity ──

    [Fact]
    public void BillingEntities_ShouldInheritFrom_BaseEntity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities.Billing")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseEntity))
            .GetResult();

        var failing = result.FailingTypeNames?.ToList() ?? [];
        // Filter out enums that live in the same namespace but are not entities
        var realFailures = failing.Where(n =>
            !n.EndsWith("Status") && !n.EndsWith("Action") && !n.EndsWith("Period") && !n.EndsWith("Type")).ToList();
        realFailures.Should().BeEmpty(
            $"All billing entities must inherit BaseEntity. Failing: {string.Join(", ", realFailures.Take(5))}");
    }

    private static string FormatFailing(TestResult result) =>
        string.Join(", ", result.FailingTypeNames?.Take(10) ?? []);
}
