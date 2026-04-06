using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

// ═══════════════════════════════════════════════════════
// PersonalDataRetentionPolicy Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class PersonalDataRetentionPolicyTests
{
    [Fact]
    public void Create_ValidInput_ReturnsActivePolicy()
    {
        var policy = PersonalDataRetentionPolicy.Create("Customer", 365, "Hash", "Email,Phone");
        policy.EntityTypeName.Should().Be("Customer");
        policy.RetentionDays.Should().Be(365);
        policy.AnonymizationStrategy.Should().Be("Hash");
        policy.FieldsToAnonymize.Should().Be("Email,Phone");
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyEntityType_Throws()
    {
        var act = () => PersonalDataRetentionPolicy.Create("", 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroRetentionDays_Throws()
    {
        var act = () => PersonalDataRetentionPolicy.Create("Order", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateRetention_ValidDays_Updates()
    {
        var policy = PersonalDataRetentionPolicy.Create("Customer", 365);
        policy.UpdateRetention(180, "KVKK güncelleme");
        policy.RetentionDays.Should().Be(180);
        policy.Notes.Should().Be("KVKK güncelleme");
    }

    [Fact]
    public void UpdateRetention_ZeroDays_Throws()
    {
        var policy = PersonalDataRetentionPolicy.Create("Customer", 365);
        var act = () => policy.UpdateRetention(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var policy = PersonalDataRetentionPolicy.Create("Test", 30);
        policy.Deactivate();
        policy.IsActive.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════
// WebhookLog Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WebhookLogTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidWebhook_SetsFields()
    {
        var log = WebhookLog.Create(_tenantId, "Trendyol", "OrderCreated",
            "{\"orderId\":123}", "sig-abc", isValid: true);

        log.TenantId.Should().Be(_tenantId);
        log.Platform.Should().Be("Trendyol");
        log.EventType.Should().Be("OrderCreated");
        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Create_InvalidWebhook_NoProcessedAt()
    {
        var log = WebhookLog.Create(_tenantId, "N11", "StockUpdate",
            "{}", null, isValid: false, error: "Invalid signature");

        log.IsValid.Should().BeFalse();
        log.ProcessedAt.Should().BeNull();
        log.Error.Should().Be("Invalid signature");
    }

    [Fact]
    public void MarkProcessed_SetsValidAndProcessedAt()
    {
        var log = WebhookLog.Create(_tenantId, "HB", "OrderUpdate",
            "{}", null, isValid: false, error: "timeout");

        log.MarkProcessed();

        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.Error.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_IncreasesCountAndSetsError()
    {
        var log = WebhookLog.Create(_tenantId, "Amazon", "RefundCreated",
            "{}", null, isValid: false);

        log.IncrementRetry("Connection timeout");
        log.RetryCount.Should().Be(1);
        log.Error.Should().Be("Connection timeout");

        log.IncrementRetry("Still failing");
        log.RetryCount.Should().Be(2);
    }

    [Fact]
    public void AnonymizePayload_ClearsPayloadAndSignature()
    {
        var log = WebhookLog.Create(_tenantId, "Trendyol", "OrderCreated",
            "{\"customer\":\"Ali\"}", "sig-123", isValid: true);

        log.AnonymizePayload();

        log.Payload.Should().Be("ANONYMIZED");
        log.Signature.Should().BeNull();
    }
}
