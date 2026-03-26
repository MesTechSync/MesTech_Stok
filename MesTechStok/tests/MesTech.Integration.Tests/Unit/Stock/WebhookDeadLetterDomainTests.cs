using FluentAssertions;
using MesTech.Domain.Entities;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G037: WebhookDeadLetter domain entity testleri.
/// Production safety — başarısız webhook'ların retry + backoff davranışı.
/// Kritik iş kuralları:
///   - Create: ilk attempt, 1 dakika sonra retry
///   - RecordRetry success: Resolved status, NextRetryAt null
///   - RecordRetry fail: exponential backoff (1m, 5m, 15m, 60m)
///   - Max attempt aşılınca: Failed status
///   - MarkManuallyResolved: admin müdahalesi
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Group", "WebhookDLQ")]
public class WebhookDeadLetterDomainTests
{
    [Fact]
    public void Create_SetsInitialState()
    {
        var dlq = WebhookDeadLetter.Create(
            "Trendyol", "ORDER_CREATED", """{"orderId":"123"}""",
            "hmac-sha256", "HTTP 500");

        dlq.Platform.Should().Be("Trendyol");
        dlq.EventType.Should().Be("ORDER_CREATED");
        dlq.AttemptCount.Should().Be(1);
        dlq.Status.Should().Be(WebhookDeadLetterStatus.Pending);
        dlq.NextRetryAt.Should().NotBeNull();
        dlq.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void RecordRetry_Success_ResolvesStatus()
    {
        var dlq = WebhookDeadLetter.Create("HB", "RETURN", "{}", null, "timeout");

        dlq.RecordRetry(success: true);

        dlq.Status.Should().Be(WebhookDeadLetterStatus.Resolved);
        dlq.ResolvedAt.Should().NotBeNull();
        dlq.NextRetryAt.Should().BeNull();
        dlq.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void RecordRetry_Fail_IncreasesAttemptAndSetsBackoff()
    {
        var dlq = WebhookDeadLetter.Create("N11", "ORDER", "{}", null, "error");

        dlq.RecordRetry(success: false, "retry fail 1");

        dlq.Status.Should().Be(WebhookDeadLetterStatus.Pending);
        dlq.AttemptCount.Should().Be(2);
        dlq.NextRetryAt.Should().NotBeNull();
        dlq.ErrorMessage.Should().Be("retry fail 1");
    }

    [Fact]
    public void RecordRetry_ExceedsMaxAttempts_FailsStatus()
    {
        var dlq = WebhookDeadLetter.Create("Ozon", "STOCK", "{}", null, "err");

        // 4 more failures → total 5 attempts (1 create + 4 retries)
        dlq.RecordRetry(false, "fail 2"); // attempt 2
        dlq.RecordRetry(false, "fail 3"); // attempt 3
        dlq.RecordRetry(false, "fail 4"); // attempt 4
        dlq.RecordRetry(false, "fail 5"); // attempt 5 = max

        dlq.Status.Should().Be(WebhookDeadLetterStatus.Failed);
        dlq.NextRetryAt.Should().BeNull();
        dlq.AttemptCount.Should().Be(5);
    }

    [Fact]
    public void MarkManuallyResolved_SetsAdminResolution()
    {
        var dlq = WebhookDeadLetter.Create("eBay", "SHIPMENT", "{}", null, "unknown");

        dlq.MarkManuallyResolved("admin@mestech.com");

        dlq.Status.Should().Be(WebhookDeadLetterStatus.ManuallyResolved);
        dlq.ProcessedBy.Should().Be("admin@mestech.com");
        dlq.ResolvedAt.Should().NotBeNull();
        dlq.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void ExponentialBackoff_FollowsPattern()
    {
        var dlq = WebhookDeadLetter.Create("CS", "ORDER", "{}", null, "err");
        var beforeRetry2 = DateTime.UtcNow;

        dlq.RecordRetry(false); // attempt 2 → 5 min backoff
        var nextRetry2 = dlq.NextRetryAt;

        dlq.RecordRetry(false); // attempt 3 → 15 min backoff
        var nextRetry3 = dlq.NextRetryAt;

        // Backoff artmalı
        nextRetry2.Should().NotBeNull();
        nextRetry3.Should().NotBeNull();
        nextRetry3!.Value.Should().BeAfter(nextRetry2!.Value);
    }
}
