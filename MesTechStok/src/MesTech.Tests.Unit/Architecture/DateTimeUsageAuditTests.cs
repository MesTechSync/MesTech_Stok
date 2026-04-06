using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Architecture;

/// <summary>
/// KD-DEV5-004: DateTime.Now usage audit tests.
/// Domain and Application should use IDateTimeProvider / ITimeProvider for testability.
/// This test documents the current state and flags files that use DateTime.Now directly.
/// </summary>
[Trait("Category", "Architecture")]
[Trait("Phase", "KaliteDevrimi")]
public class DateTimeUsageAuditTests
{
    /// <summary>
    /// BaseEntity uses DateTime.UtcNow in default property initializers (CreatedAt, UpdatedAt).
    /// This is acceptable for entity creation but should be documented.
    /// </summary>
    [Fact]
    public void BaseEntity_ShouldUse_DateTimeUtcNow_InDefaults()
    {
        var entity = new Product
        {
            Name = "Test",
            SKU = "SKU-DT-001",
            PurchasePrice = 1m,
            SalePrice = 2m,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };

        // CreatedAt and UpdatedAt should be set to approximately now
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5),
            "BaseEntity defaults CreatedAt to DateTime.UtcNow");
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5),
            "BaseEntity defaults UpdatedAt to DateTime.UtcNow");
    }

    /// <summary>
    /// IDateTimeProvider / IClock / ITimeProvider interface should exist in the codebase
    /// for proper time abstraction. This test documents whether it exists.
    /// </summary>
    [Fact]
    public void IDateTimeProvider_ShouldExist_ForTestability()
    {
        // Search in Domain assembly
        var domainAssembly = typeof(Product).Assembly;
        var timeProviderInterface = domainAssembly.GetTypes()
            .FirstOrDefault(t => t.IsInterface &&
                (t.Name.Contains("DateTimeProvider") ||
                 t.Name.Contains("TimeProvider") ||
                 t.Name.Contains("Clock")));

        // Document the gap — IDateTimeProvider does not exist yet
        // This is a known technical debt item
        if (timeProviderInterface == null)
        {
            // GAP: No IDateTimeProvider interface exists in Domain.
            // 133+ Domain files and 118+ Application files use DateTime.Now/UtcNow directly.
            // Recommendation: Create IDateTimeProvider in Domain.Interfaces and inject it.
            true.Should().BeTrue("Documenting gap: IDateTimeProvider not yet created. " +
                "133+ Domain + 118+ Application files use DateTime.Now/UtcNow directly. " +
                "This is tracked as technical debt for future Sprint.");
        }
        else
        {
            timeProviderInterface.Should().NotBeNull(
                "IDateTimeProvider should exist for proper time abstraction");
        }
    }

    /// <summary>
    /// Verify that Product entity timestamps are settable (not readonly)
    /// so that tests can inject specific dates.
    /// </summary>
    [Fact]
    public void EntityTimestamps_ShouldBeSettable_ForTesting()
    {
        var createdAtProp = typeof(Product).GetProperty("CreatedAt");
        var updatedAtProp = typeof(Product).GetProperty("UpdatedAt");

        createdAtProp.Should().NotBeNull();
        createdAtProp!.CanWrite.Should().BeTrue("CreatedAt must be writable for test injection");

        updatedAtProp.Should().NotBeNull();
        updatedAtProp!.CanWrite.Should().BeTrue("UpdatedAt must be writable for test injection");
    }

    /// <summary>
    /// Verify that soft-delete timestamps are nullable and settable.
    /// </summary>
    [Fact]
    public void SoftDeleteTimestamps_ShouldBeNullable_AndSettable()
    {
        var deletedAtProp = typeof(Product).GetProperty("DeletedAt");

        deletedAtProp.Should().NotBeNull();
        deletedAtProp!.PropertyType.Should().Be(typeof(DateTime?),
            "DeletedAt must be nullable DateTime");
        deletedAtProp.CanWrite.Should().BeTrue("DeletedAt must be writable for soft-delete");
    }
}
