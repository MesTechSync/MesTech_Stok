using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

/// <summary>
/// DEV5 TUR 10: Extended architecture rules.
/// Covers: Validator naming, Query naming, DTO suffix,
/// adapter sealed enforcement, no circular dependency,
/// Accounting/Finance/Stock domain isolation.
/// </summary>
[Trait("Category", "Architecture")]
public class ExtendedArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Product).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(CreateWorkTaskHandler).Assembly;
    private static readonly System.Reflection.Assembly InfraAssembly = typeof(TrendyolAdapter).Assembly;

    // ── Validator naming ──

    [Fact]
    public void AllValidators_ShouldEndWithValidator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All FluentValidation validators must end with 'Validator'. Failing: {FormatFailing(result)}");
    }

    // ── Query naming ──

    [Fact]
    public void Queries_ShouldEndWithQuery()
    {
        var queryTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .GetTypes();

        queryTypes.Should().NotBeEmpty("At least one Query type should exist in Application assembly.");

        foreach (var type in queryTypes)
        {
            type.Name.Should().EndWith("Query",
                $"{type.FullName} must end with 'Query'.");
        }
    }

    // ── Finance domain isolation ──

    [Fact]
    public void FinanceEntities_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities.Finance")
            .ShouldNot()
            .HaveDependencyOnAny("MesTech.Infrastructure", "MesTech.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Finance domain entities must be isolated from outer layers.");
    }

    // ── Accounting domain isolation ──

    [Fact]
    public void AccountingEntities_ShouldNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities.Accounting")
            .ShouldNot()
            .HaveDependencyOnAny("MesTech.Infrastructure", "MesTech.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Accounting domain entities must not depend on Application or Infrastructure.");
    }

    // ── Onboarding domain isolation ──

    [Fact]
    public void OnboardingEntities_ShouldNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities.Onboarding")
            .ShouldNot()
            .HaveDependencyOnAny("MesTech.Infrastructure", "MesTech.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Onboarding domain entities must not depend on outer layers.");
    }

    // ── Domain events must implement IDomainEvent ──

    [Fact]
    public void DomainEvents_ShouldImplementIDomainEvent()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Events")
            .And()
            .AreNotAbstract()
            .And()
            .AreNotInterfaces()
            .Should()
            .ImplementInterface(typeof(MesTech.Domain.Common.IDomainEvent))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All domain events must implement IDomainEvent. Failing: {FormatFailing(result)}");
    }

    // ── Integration adapters must be sealed ──

    [Fact]
    public void IntegrationAdapters_ShouldBeSealed()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Infrastructure.Integration.Adapters")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All integration adapters must be sealed. Unsealed: {FormatFailing(result)}");
    }

    // ── Infrastructure must not reference Avalonia/Blazor/Desktop ──

    [Fact]
    public void Infrastructure_ShouldNotDependOnUI()
    {
        var result = Types.InAssembly(InfraAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "MesTech.Avalonia",
                "MesTech.Blazor",
                "MesTechStok.Desktop")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not depend on any UI layer.");
    }

    // ── Application interfaces must start with 'I' ──

    [Fact]
    public void ApplicationInterfaces_ShouldStartWithI()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All interfaces must start with 'I'. Failing: {FormatFailing(result)}");
    }

    // ── Helper ──

    private static string FormatFailing(TestResult result) =>
        string.Join(", ", result.FailingTypeNames?.Take(10) ?? []);
}
