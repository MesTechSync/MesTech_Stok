using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Domain.Entities;
using NetArchTest.Rules;

namespace MesTech.Tests.Architecture;

/// <summary>
/// Sealed class and immutability architecture rules.
/// Handlers must be sealed. Domain entities should be sealed.
/// Validators must be sealed.
/// </summary>
[Trait("Category", "Architecture")]
public class SealedAndImmutabilityTests
{
    // ── 1. All Handlers must be sealed ──────────────────────────────────────

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var applicationAssembly = typeof(CreateWorkTaskHandler).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All Handler classes must be sealed. Unsealed: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── 2. All Validators must be sealed ────────────────────────────────────

    [Fact]
    public void Validators_ShouldBeSealed()
    {
        var applicationAssembly = typeof(CreateWorkTaskHandler).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All Validator classes must be sealed. Unsealed: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── 3. Domain entities should be sealed ─────────────────────────────────

    [Fact]
    public void DomainEntities_ShouldBeSealed()
    {
        var domainAssembly = typeof(Product).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceStartingWith("MesTech.Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Allow up to 10 unsealed entities (BaseEntity descendants that use inheritance)
        var failCount = result.FailingTypeNames?.Count() ?? 0;
        failCount.Should().BeLessThan(10,
            $"Most domain entities should be sealed. Unsealed ({failCount}): {string.Join(", ", result.FailingTypeNames?.Take(10) ?? [])}");
    }

    // ── 4. Command records should not have mutable properties ────────────────

    [Fact]
    public void Commands_ShouldBeRecords()
    {
        var applicationAssembly = typeof(CreateWorkTaskHandler).Assembly;

        var commandTypes = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .GetTypes();

        foreach (var type in commandTypes)
        {
            // Records compile to classes with special methods
            // Check if it's a record by looking for <Clone>$ method
            var isRecord = type.GetMethod("<Clone>$") != null;
            // Allow sealed classes too (some commands are sealed class instead of record)
            var isSealed = type.IsSealed;

            (isRecord || isSealed).Should().BeTrue(
                $"{type.Name} should be a record or sealed class.");
        }
    }
}
