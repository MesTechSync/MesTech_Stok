using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 8: Entity domain logic batch 3
// WebhookLog (retry+KVKK), CrmContact (factory+link),
// ProductSetItem (factory guard)
// ════════════════════════════════════════════════════════

#region WebhookLog — Retry + KVKK Anonymization

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WebhookLogDomainTests
{
    [Fact]
    public void Create_ValidWebhook_SetsDefaults()
    {
        var log = WebhookLog.Create(Guid.NewGuid(), "Trendyol", "order.created",
            "{\"orderId\":123}", "sha256=abc", isValid: true);

        log.Platform.Should().Be("Trendyol");
        log.EventType.Should().Be("order.created");
        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.RetryCount.Should().Be(0);
        log.Error.Should().BeNull();
    }

    [Fact]
    public void Create_InvalidWebhook_ProcessedAtIsNull()
    {
        var log = WebhookLog.Create(Guid.NewGuid(), "HB", "order", "{}", null,
            isValid: false, error: "Invalid signature");

        log.IsValid.Should().BeFalse();
        log.ProcessedAt.Should().BeNull();
        log.Error.Should().Be("Invalid signature");
    }

    [Fact]
    public void MarkProcessed_SetsValidAndClearsError()
    {
        var log = WebhookLog.Create(Guid.NewGuid(), "N11", "stock", "{}", null,
            isValid: false, error: "Timeout");

        log.MarkProcessed();

        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.Error.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_IncreasesCountAndSetsError()
    {
        var log = WebhookLog.Create(Guid.NewGuid(), "Amazon", "fulfillment", "{}", null, isValid: false);

        log.IncrementRetry("Connection refused");
        log.RetryCount.Should().Be(1);
        log.Error.Should().Be("Connection refused");

        log.IncrementRetry("Timeout");
        log.RetryCount.Should().Be(2);
        log.Error.Should().Be("Timeout");
    }

    [Fact]
    public void AnonymizePayload_ClearsPayloadAndSignature()
    {
        var log = WebhookLog.Create(Guid.NewGuid(), "Trendyol", "order.created",
            "{\"customer\":\"Ali Veli\",\"phone\":\"+905551234567\"}", "sha256=secret", isValid: true);

        log.AnonymizePayload();

        log.Payload.Should().Be("ANONYMIZED");
        log.Signature.Should().BeNull();
    }
}

#endregion

#region CrmContact — Factory + Link

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CrmContactDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Ali Veli", ContactType.Individual,
            email: "ali@test.com", phone: "+905551234567");

        contact.FullName.Should().Be("Ali Veli");
        contact.Type.Should().Be(ContactType.Individual);
        contact.Email.Should().Be("ali@test.com");
        contact.IsActive.Should().BeTrue();
        contact.CustomerId.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => CrmContact.Create(Guid.NewGuid(), "", ContactType.Individual);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_CompanyType_SetsType()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "ABC Ltd", ContactType.Company,
            company: "ABC Ltd Şti");

        contact.Type.Should().Be(ContactType.Company);
        contact.Company.Should().Be("ABC Ltd Şti");
    }

    [Fact]
    public void LinkToCustomer_SetsCustomerId()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        var customerId = Guid.NewGuid();

        contact.LinkToCustomer(customerId);

        contact.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        contact.UpdateNotes("VIP müşteri — öncelikli destek");
        contact.Notes.Should().Contain("VIP");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        contact.Deactivate();
        contact.IsActive.Should().BeFalse();
    }
}

#endregion

#region ProductSetItem — Factory Guard

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class ProductSetItemDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var item = ProductSetItem.Create(Guid.NewGuid(), Guid.NewGuid(), 3);
        item.Quantity.Should().Be(3);
    }

    [Fact]
    public void Create_EmptyProductSetId_Throws()
    {
        var act = () => ProductSetItem.Create(Guid.Empty, Guid.NewGuid(), 1);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductSetId*");
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        var act = () => ProductSetItem.Create(Guid.NewGuid(), Guid.Empty, 1);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    [Fact]
    public void Create_ZeroQuantity_Throws()
    {
        var act = () => ProductSetItem.Create(Guid.NewGuid(), Guid.NewGuid(), 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeQuantity_Throws()
    {
        var act = () => ProductSetItem.Create(Guid.NewGuid(), Guid.NewGuid(), -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

#endregion
