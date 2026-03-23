using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// SyncRetryItem entity testleri.
/// Create, IncrementRetry, MaxRetry, CalculateNextRetry, MarkAsResolved.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "SyncRetry")]
public class SyncRetryItemTests
{
    // ══════════════════════════════════════════════════════════════════════════
    // Create / Default State Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "New SyncRetryItem — defaults are correct")]
    public void New_SyncRetryItem_HasCorrectDefaults()
    {
        // Arrange & Act
        var item = new SyncRetryItem
        {
            TenantId = Guid.NewGuid(),
            SyncType = "ProductSync",
            ItemId = "SKU-001",
            ItemType = "Product"
        };

        // Assert
        item.RetryCount.Should().Be(0);
        item.MaxRetries.Should().Be(3);
        item.IsResolved.Should().BeFalse();
        item.ResolvedUtc.Should().BeNull();
        item.LastError.Should().BeEmpty();
        item.ErrorCategory.Should().BeEmpty();
        item.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        item.SyncType.Should().Be("ProductSync");
        item.ItemId.Should().Be("SKU-001");
        item.ItemType.Should().Be("Product");
    }

    [Fact(DisplayName = "New SyncRetryItem — optional fields default to null")]
    public void New_SyncRetryItem_OptionalFieldsAreNull()
    {
        var item = new SyncRetryItem();

        item.ItemData.Should().BeNull();
        item.CorrelationId.Should().BeNull();
        item.AdditionalInfo.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IncrementRetry Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "IncrementRetry — increments count and sets error details")]
    public void IncrementRetry_IncrementsCountAndSetsErrorDetails()
    {
        // Arrange
        var item = new SyncRetryItem();

        // Act
        item.IncrementRetry("Connection timeout", "Network");

        // Assert
        item.RetryCount.Should().Be(1);
        item.LastError.Should().Be("Connection timeout");
        item.ErrorCategory.Should().Be("Network");
        item.LastRetryUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        item.NextRetryUtc.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncrementRetry — called multiple times accumulates count")]
    public void IncrementRetry_MultipleTimesAccumulatesCount()
    {
        var item = new SyncRetryItem();

        item.IncrementRetry("Error 1", "API");
        item.IncrementRetry("Error 2", "API");
        item.IncrementRetry("Error 3", "Timeout");

        item.RetryCount.Should().Be(3);
        item.LastError.Should().Be("Error 3");
        item.ErrorCategory.Should().Be("Timeout");
    }

    [Fact(DisplayName = "IncrementRetry — overwrites previous error details")]
    public void IncrementRetry_OverwritesPreviousErrorDetails()
    {
        var item = new SyncRetryItem();

        item.IncrementRetry("First error", "Network");
        item.IncrementRetry("Second error", "Validation");

        item.LastError.Should().Be("Second error");
        item.ErrorCategory.Should().Be("Validation");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MaxRetryExceeded Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "MaxRetryExceeded — RetryCount exceeds MaxRetries after enough increments")]
    public void MaxRetryExceeded_RetryCountExceedsMaxRetries()
    {
        var item = new SyncRetryItem { MaxRetries = 3 };

        item.IncrementRetry("err1", "cat");
        item.IncrementRetry("err2", "cat");
        item.IncrementRetry("err3", "cat");

        item.RetryCount.Should().Be(3);
        (item.RetryCount >= item.MaxRetries).Should().BeTrue(
            "RetryCount should reach or exceed MaxRetries");
    }

    [Fact(DisplayName = "MaxRetries — custom value is respected")]
    public void MaxRetries_CustomValueIsRespected()
    {
        var item = new SyncRetryItem { MaxRetries = 5 };

        item.MaxRetries.Should().Be(5);
    }

    [Fact(DisplayName = "MaxRetryExceeded — RetryCount below MaxRetries means not exceeded")]
    public void MaxRetryExceeded_BelowMaxRetries_NotExceeded()
    {
        var item = new SyncRetryItem { MaxRetries = 3 };
        item.IncrementRetry("err", "cat");

        (item.RetryCount < item.MaxRetries).Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CalculateNextRetry Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "CalculateNextRetry — at RetryCount 0 schedules ~60 seconds ahead")]
    public void CalculateNextRetry_AtRetryCount0_Schedules60SecondsAhead()
    {
        var item = new SyncRetryItem();
        // RetryCount = 0 => backoff = 2^0 * 60 = 60 seconds

        item.CalculateNextRetry();

        item.NextRetryUtc.Should().NotBeNull();
        var expected = DateTime.UtcNow.AddSeconds(60);
        item.NextRetryUtc!.Value.Should().BeCloseTo(expected, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "CalculateNextRetry — exponential backoff increases with retry count")]
    public void CalculateNextRetry_ExponentialBackoffIncreasesWithRetryCount()
    {
        var item = new SyncRetryItem();

        // RetryCount = 0 => 60s
        item.CalculateNextRetry();
        var firstNext = item.NextRetryUtc!.Value;

        // RetryCount = 1 => 120s
        item.IncrementRetry("err", "cat");
        var secondNext = item.NextRetryUtc!.Value;

        // Second retry should be scheduled further in the future
        secondNext.Should().BeAfter(firstNext);
    }

    [Fact(DisplayName = "CalculateNextRetry — caps at 86400 seconds (24 hours)")]
    public void CalculateNextRetry_CapsAt86400Seconds()
    {
        var item = new SyncRetryItem();

        // Simulate many retries so backoff exceeds 86400
        for (int i = 0; i < 20; i++)
            item.IncrementRetry("err", "cat");

        // 2^20 * 60 = 62,914,560 >> 86400, so should be capped
        var maxExpected = DateTime.UtcNow.AddSeconds(86400);
        item.NextRetryUtc!.Value.Should().BeCloseTo(maxExpected, TimeSpan.FromSeconds(5));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MarkAsResolved Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "MarkAsResolved — sets IsResolved and clears NextRetryUtc")]
    public void MarkAsResolved_SetsIsResolvedAndClearsNextRetry()
    {
        var item = new SyncRetryItem();
        item.IncrementRetry("err", "cat");
        item.NextRetryUtc.Should().NotBeNull("precondition: NextRetryUtc set by IncrementRetry");

        item.MarkAsResolved();

        item.IsResolved.Should().BeTrue();
        item.ResolvedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        item.NextRetryUtc.Should().BeNull();
    }

    [Fact(DisplayName = "MarkAsResolved — idempotent call does not throw")]
    public void MarkAsResolved_IdempotentCall_DoesNotThrow()
    {
        var item = new SyncRetryItem();

        item.MarkAsResolved();
        var act = () => item.MarkAsResolved();

        act.Should().NotThrow();
        item.IsResolved.Should().BeTrue();
    }

    [Fact(DisplayName = "MarkAsResolved — preserves retry history")]
    public void MarkAsResolved_PreservesRetryHistory()
    {
        var item = new SyncRetryItem();
        item.IncrementRetry("err1", "Network");
        item.IncrementRetry("err2", "API");

        item.MarkAsResolved();

        item.RetryCount.Should().Be(2);
        item.LastError.Should().Be("err2");
        item.ErrorCategory.Should().Be("API");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ITenantEntity Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "TenantId — can be set and retrieved")]
    public void TenantId_CanBeSetAndRetrieved()
    {
        var tenantId = Guid.NewGuid();
        var item = new SyncRetryItem { TenantId = tenantId };

        item.TenantId.Should().Be(tenantId);
    }
}
