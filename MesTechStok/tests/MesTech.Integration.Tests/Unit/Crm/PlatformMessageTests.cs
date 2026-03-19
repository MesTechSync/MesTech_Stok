using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Crm;

/// <summary>
/// EMR-09 ALAN-F — PlatformMessage domain method testleri.
/// Entity uzerindeki MarkAsRead, SetReply, SetAiSuggestion, Archive metodlarini dogrular.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Crm")]
[Trait("Group", "PlatformMessage")]
public class PlatformMessageTests
{
    private static PlatformMessage CreateUnreadMessage() => new()
    {
        TenantId = Guid.NewGuid(),
        Platform = PlatformType.Trendyol,
        ExternalMessageId = "TY-MSG-001",
        SenderName = "Ahmet Yilmaz",
        Subject = "Urun iade talebi",
        Body = "Urun iade etmek istiyorum.",
        Direction = MessageDirection.Incoming,
        Status = MessageStatus.Unread,
        ReceivedAt = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════
    // 1. MarkAsRead — Unread → Read
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkAsRead_ChangesStatus()
    {
        // Arrange
        var message = CreateUnreadMessage();
        message.Status.Should().Be(MessageStatus.Unread);

        // Act
        message.MarkAsRead();

        // Assert
        message.Status.Should().Be(MessageStatus.Read);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. SetReply — Reply, RepliedAt, Status guncellenir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SetReply_UpdatesStatusAndTimestamp()
    {
        // Arrange
        var message = CreateUnreadMessage();
        var beforeReply = DateTime.UtcNow;

        // Act
        message.SetReply("Iade talebiniz onaylandi.", "admin@mestech.com");

        // Assert
        message.Status.Should().Be(MessageStatus.Replied);
        message.Reply.Should().Be("Iade talebiniz onaylandi.");
        message.RepliedBy.Should().Be("admin@mestech.com");
        message.RepliedAt.Should().NotBeNull();
        message.RepliedAt!.Value.Should().BeOnOrAfter(beforeReply);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. SetAiSuggestion — AI yanit onerisi saklanir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SetAiSuggestion_StoresSuggestion()
    {
        // Arrange
        var message = CreateUnreadMessage();
        var suggestion = "Sayin musterimiz, iade talebinizi inceliyoruz. En kisa surede geri donus yapacagiz.";

        // Act
        message.SetAiSuggestion(suggestion);

        // Assert
        message.AiSuggestedReply.Should().Be(suggestion);
        message.AiSuggestedReply.Should().NotBeNullOrWhiteSpace();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. Archive — Status Archived olur
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Archive_ChangesStatus()
    {
        // Arrange
        var message = CreateUnreadMessage();

        // Act
        message.Archive();

        // Assert
        message.Status.Should().Be(MessageStatus.Archived);
    }
}
