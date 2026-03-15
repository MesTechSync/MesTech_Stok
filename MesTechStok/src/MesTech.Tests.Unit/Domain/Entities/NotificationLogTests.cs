using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// NotificationLog entity testleri.
/// Factory method, state machine gecisleri, ve hatali gecis senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
[Trait("Phase", "Dalga12")]
public class NotificationLogTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ══════════════════════════════════════════════════════════════════════════
    // Create Factory Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Create — valid parameters produce Pending notification")]
    public void Create_ValidParameters_ReturnsPendingNotification()
    {
        // Act
        var log = NotificationLog.Create(
            _tenantId,
            NotificationChannel.Telegram,
            "user@example.com",
            "OrderConfirmation",
            "Siparisiz #ORD-1001 onaylandi.");

        // Assert
        log.TenantId.Should().Be(_tenantId);
        log.Channel.Should().Be(NotificationChannel.Telegram);
        log.Recipient.Should().Be("user@example.com");
        log.TemplateName.Should().Be("OrderConfirmation");
        log.Content.Should().Contain("ORD-1001");
        log.Status.Should().Be(NotificationStatus.Pending);
        log.SentAt.Should().BeNull();
        log.DeliveredAt.Should().BeNull();
        log.ReadAt.Should().BeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Theory(DisplayName = "Create — empty/null required fields throw ArgumentException")]
    [InlineData("", "Template", "Content")]
    [InlineData("recipient", "", "Content")]
    [InlineData("recipient", "Template", "")]
    [InlineData(null, "Template", "Content")]
    [InlineData("recipient", null, "Content")]
    [InlineData("recipient", "Template", null)]
    public void Create_EmptyRequiredFields_ThrowsArgumentException(
        string? recipient, string? template, string? content)
    {
        var act = () => NotificationLog.Create(
            _tenantId,
            NotificationChannel.Email,
            recipient!,
            template!,
            content!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Create — all notification channels supported")]
    [InlineData(NotificationChannel.WhatsApp)]
    [InlineData(NotificationChannel.Telegram)]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.Push)]
    [InlineData(NotificationChannel.SMS)]
    public void Create_AllChannels_Supported(NotificationChannel channel)
    {
        var log = NotificationLog.Create(
            _tenantId, channel, "test@test.com", "Test", "Test content");

        log.Channel.Should().Be(channel);
        log.Status.Should().Be(NotificationStatus.Pending);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Happy Path: Pending -> Sent -> Delivered -> Read
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "MarkAsSent — from Pending transitions to Sent")]
    public void MarkAsSent_FromPending_TransitionsToSent()
    {
        var log = CreatePendingLog();

        log.MarkAsSent();

        log.Status.Should().Be(NotificationStatus.Sent);
        log.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "MarkAsDelivered — from Sent transitions to Delivered")]
    public void MarkAsDelivered_FromSent_TransitionsToDelivered()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();

        log.MarkAsDelivered();

        log.Status.Should().Be(NotificationStatus.Delivered);
        log.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "MarkAsRead — from Delivered transitions to Read")]
    public void MarkAsRead_FromDelivered_TransitionsToRead()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();
        log.MarkAsDelivered();

        log.MarkAsRead();

        log.Status.Should().Be(NotificationStatus.Read);
        log.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Full lifecycle — Pending -> Sent -> Delivered -> Read")]
    public void FullLifecycle_PendingToRead_AllTimestampsSet()
    {
        var log = CreatePendingLog();

        log.MarkAsSent();
        log.MarkAsDelivered();
        log.MarkAsRead();

        log.Status.Should().Be(NotificationStatus.Read);
        log.SentAt.Should().NotBeNull();
        log.DeliveredAt.Should().NotBeNull();
        log.ReadAt.Should().NotBeNull();
        log.ErrorMessage.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MarkAsFailed Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "MarkAsFailed — from Pending transitions to Failed with reason")]
    public void MarkAsFailed_FromPending_TransitionsToFailed()
    {
        var log = CreatePendingLog();

        log.MarkAsFailed("SMTP connection timeout");

        log.Status.Should().Be(NotificationStatus.Failed);
        log.ErrorMessage.Should().Be("SMTP connection timeout");
    }

    [Fact(DisplayName = "MarkAsFailed — from Sent transitions to Failed")]
    public void MarkAsFailed_FromSent_TransitionsToFailed()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();

        log.MarkAsFailed("Recipient unreachable");

        log.Status.Should().Be(NotificationStatus.Failed);
        log.ErrorMessage.Should().Be("Recipient unreachable");
    }

    [Fact(DisplayName = "MarkAsFailed — empty reason throws ArgumentException")]
    public void MarkAsFailed_EmptyReason_ThrowsArgumentException()
    {
        var log = CreatePendingLog();
        var act = () => log.MarkAsFailed("");
        act.Should().Throw<ArgumentException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Invalid State Transitions
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "MarkAsRead — from Pending throws InvalidOperationException")]
    public void MarkAsRead_FromPending_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        var act = () => log.MarkAsRead();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pending*");
    }

    [Fact(DisplayName = "MarkAsRead — from Sent throws InvalidOperationException")]
    public void MarkAsRead_FromSent_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();

        var act = () => log.MarkAsRead();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sent*");
    }

    [Fact(DisplayName = "MarkAsDelivered — from Pending throws InvalidOperationException")]
    public void MarkAsDelivered_FromPending_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        var act = () => log.MarkAsDelivered();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pending*");
    }

    [Fact(DisplayName = "MarkAsSent — from Delivered throws InvalidOperationException")]
    public void MarkAsSent_FromDelivered_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();
        log.MarkAsDelivered();

        var act = () => log.MarkAsSent();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Delivered*");
    }

    [Fact(DisplayName = "MarkAsFailed — from Delivered throws InvalidOperationException")]
    public void MarkAsFailed_FromDelivered_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();
        log.MarkAsDelivered();

        var act = () => log.MarkAsFailed("Late failure");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Delivered*");
    }

    [Fact(DisplayName = "MarkAsFailed — from Read throws InvalidOperationException")]
    public void MarkAsFailed_FromRead_ThrowsInvalidOperationException()
    {
        var log = CreatePendingLog();
        log.MarkAsSent();
        log.MarkAsDelivered();
        log.MarkAsRead();

        var act = () => log.MarkAsFailed("Too late");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Read*");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ToString
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "ToString — includes channel, recipient, template, and status")]
    public void ToString_ContainsAllRelevantInfo()
    {
        var log = NotificationLog.Create(
            _tenantId, NotificationChannel.WhatsApp, "+905551234567", "ShipmentUpdate", "Kargonuz yola cikti");

        var str = log.ToString();

        str.Should().Contain("WhatsApp");
        str.Should().Contain("+905551234567");
        str.Should().Contain("ShipmentUpdate");
        str.Should().Contain("Pending");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper
    // ══════════════════════════════════════════════════════════════════════════

    private NotificationLog CreatePendingLog()
    {
        return NotificationLog.Create(
            _tenantId,
            NotificationChannel.Email,
            "test@mestech.com",
            "TestTemplate",
            "Test notification content");
    }
}
