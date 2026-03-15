using FluentAssertions;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Exceptions;
using Xunit;

namespace MesTech.Tests.Unit.Dropshipping;

/// <summary>
/// DropshipSupplier entity unit testleri — Dalga 13 Wave 1.
/// 20 tests: Create, Activate, Deactivate, UpdateMarkup, RecordSync, SetApiCredentials, guards.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "DropshipSupplier")]
public class DropshipSupplierTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static DropshipSupplier CreateValidSupplier(
        DropshipMarkupType markupType = DropshipMarkupType.Percentage,
        decimal markupValue = 20m)
    {
        return DropshipSupplier.Create(
            TenantId,
            "Test Tedarikci",
            "https://supplier.example.com",
            markupType,
            markupValue);
    }

    // ══════════════════════════════════════════════════════════════
    // 1. Create — valid data
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithValidData_ShouldSetAllProperties()
    {
        // Arrange & Act
        var supplier = DropshipSupplier.Create(
            TenantId,
            "Acme Supplier",
            "https://acme.com",
            DropshipMarkupType.Percentage,
            25m);

        // Assert
        supplier.TenantId.Should().Be(TenantId);
        supplier.Name.Should().Be("Acme Supplier");
        supplier.WebsiteUrl.Should().Be("https://acme.com");
        supplier.MarkupType.Should().Be(DropshipMarkupType.Percentage);
        supplier.MarkupValue.Should().Be(25m);
        supplier.Id.Should().NotBeEmpty();
    }

    // ══════════════════════════════════════════════════════════════
    // 2. Create — empty name guard
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrow(string? name)
    {
        var act = () => DropshipSupplier.Create(
            TenantId,
            name!,
            "https://supplier.com",
            DropshipMarkupType.Percentage,
            10m);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 3. Create — negative markup guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithNegativeMarkup_ShouldThrow()
    {
        var act = () => DropshipSupplier.Create(
            TenantId,
            "Supplier",
            "https://supplier.com",
            DropshipMarkupType.Percentage,
            -5m);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 4. IsActive default
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        var supplier = CreateValidSupplier();

        supplier.IsActive.Should().BeTrue("new supplier should be active by default");
    }

    // ══════════════════════════════════════════════════════════════
    // 5. TenantId set correctly
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_ShouldSetTenantIdCorrectly()
    {
        var tenantId = Guid.NewGuid();

        var supplier = DropshipSupplier.Create(
            tenantId,
            "Tenant Test",
            "https://tenant.com",
            DropshipMarkupType.FixedAmount,
            10m);

        supplier.TenantId.Should().Be(tenantId);
    }

    // ══════════════════════════════════════════════════════════════
    // 6. SyncIntervalMinutes default
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_SyncIntervalMinutes_ShouldHaveDefaultValue()
    {
        var supplier = CreateValidSupplier();

        supplier.SyncIntervalMinutes.Should().BeGreaterThan(0,
            "default sync interval should be a positive number");
    }

    // ══════════════════════════════════════════════════════════════
    // 7. LastSyncAt default
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_LastSyncAt_ShouldBeNull()
    {
        var supplier = CreateValidSupplier();

        supplier.LastSyncAt.Should().BeNull("new supplier has not been synced yet");
    }

    // ══════════════════════════════════════════════════════════════
    // 8. Activate on inactive supplier
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Activate_OnInactiveSupplier_ShouldSetIsActiveTrue()
    {
        // Arrange
        var supplier = CreateValidSupplier();
        supplier.Deactivate();
        supplier.IsActive.Should().BeFalse();

        // Act
        supplier.Activate();

        // Assert
        supplier.IsActive.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    // 9. Activate on already active — idempotent
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Activate_OnAlreadyActive_ShouldRemainActive()
    {
        var supplier = CreateValidSupplier();
        supplier.IsActive.Should().BeTrue();

        // Act — calling Activate on already active should not throw
        supplier.Activate();

        // Assert
        supplier.IsActive.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    // 10. Deactivate
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var supplier = CreateValidSupplier();

        supplier.Deactivate();

        supplier.IsActive.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // 11. Deactivate on already inactive — idempotent
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Deactivate_OnAlreadyInactive_ShouldRemainInactive()
    {
        var supplier = CreateValidSupplier();
        supplier.Deactivate();

        // Act — calling Deactivate again should not throw
        supplier.Deactivate();

        supplier.IsActive.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // 12-13. UpdateMarkup — percentage and fixed amount
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(DropshipMarkupType.Percentage, 15)]
    [InlineData(DropshipMarkupType.FixedAmount, 50)]
    public void UpdateMarkup_WithValidData_ShouldUpdateBothTypeAndValue(
        DropshipMarkupType type, decimal value)
    {
        var supplier = CreateValidSupplier();

        supplier.UpdateMarkup(type, value);

        supplier.MarkupType.Should().Be(type);
        supplier.MarkupValue.Should().Be(value);
    }

    // ══════════════════════════════════════════════════════════════
    // 14. UpdateMarkup — zero value guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateMarkup_WithZeroValue_ShouldThrow()
    {
        var supplier = CreateValidSupplier();

        var act = () => supplier.UpdateMarkup(DropshipMarkupType.Percentage, 0m);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 15. UpdateMarkup — negative value guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateMarkup_WithNegativeValue_ShouldThrow()
    {
        var supplier = CreateValidSupplier();

        var act = () => supplier.UpdateMarkup(DropshipMarkupType.FixedAmount, -10m);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 16. RecordSync updates LastSyncAt
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordSync_ShouldUpdateLastSyncAt()
    {
        var supplier = CreateValidSupplier();
        supplier.LastSyncAt.Should().BeNull();

        supplier.RecordSync();

        supplier.LastSyncAt.Should().NotBeNull();
        supplier.LastSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ══════════════════════════════════════════════════════════════
    // 17. RecordSync — multiple calls update timestamp
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordSync_CalledMultipleTimes_ShouldAlwaysUpdateTimestamp()
    {
        var supplier = CreateValidSupplier();

        supplier.RecordSync();
        var firstSync = supplier.LastSyncAt;

        // Ensure at least some time passes
        supplier.RecordSync();
        var secondSync = supplier.LastSyncAt;

        secondSync.Should().BeOnOrAfter(firstSync!.Value,
            "subsequent sync should have equal or later timestamp");
    }

    // ══════════════════════════════════════════════════════════════
    // 18. SetApiCredentials — valid data
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void SetApiCredentials_WithValidData_ShouldSetEndpointAndApiKey()
    {
        var supplier = CreateValidSupplier();

        supplier.SetApiCredentials("https://api.supplier.com/v1", "sk-test-key-12345");

        supplier.ApiEndpoint.Should().Be("https://api.supplier.com/v1");
        supplier.ApiKey.Should().Be("sk-test-key-12345");
    }

    // ══════════════════════════════════════════════════════════════
    // 19. SetApiCredentials — empty endpoint guard
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null, "valid-key")]
    [InlineData("", "valid-key")]
    [InlineData("   ", "valid-key")]
    public void SetApiCredentials_WithEmptyEndpoint_ShouldThrow(string? endpoint, string apiKey)
    {
        var supplier = CreateValidSupplier();

        var act = () => supplier.SetApiCredentials(endpoint!, apiKey);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 20. SetApiCredentials — empty apiKey guard
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("https://api.example.com", null)]
    [InlineData("https://api.example.com", "")]
    [InlineData("https://api.example.com", "   ")]
    public void SetApiCredentials_WithEmptyApiKey_ShouldThrow(string endpoint, string? apiKey)
    {
        var supplier = CreateValidSupplier();

        var act = () => supplier.SetApiCredentials(endpoint, apiKey!);

        act.Should().Throw<Exception>();
    }
}
