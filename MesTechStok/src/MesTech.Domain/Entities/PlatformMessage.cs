using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform mesaji entity'si — pazaryeri mesajlarini temsil eder.
/// </summary>
public sealed class PlatformMessage : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public PlatformType Platform { get; set; }
    public string ExternalMessageId { get; set; } = string.Empty;
    public string? ExternalConversationId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public MessageStatus Status { get; private set; } = MessageStatus.Unread;
    public string? AiSuggestedReply { get; private set; }
    public string? Reply { get; private set; }
    public DateTime? RepliedAt { get; private set; }
    public string? RepliedBy { get; private set; }
    public DateTime ReceivedAt { get; set; }

    public void MarkAsRead()
    {
        if (Status == MessageStatus.Unread)
        {
            Status = MessageStatus.Read;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetReply(string reply, string repliedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reply);
        ArgumentException.ThrowIfNullOrWhiteSpace(repliedBy);

        Reply = reply;
        RepliedBy = repliedBy;
        RepliedAt = DateTime.UtcNow;
        Status = MessageStatus.Replied;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiSuggestion(string suggestion)
    {
        AiSuggestedReply = suggestion;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = MessageStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Yeni gelen mesaj alindiktan sonra domain event uretir.
    /// </summary>
    public void RaiseReceivedEvent()
    {
        RaiseDomainEvent(new PlatformMessageReceivedEvent(Id, TenantId, Platform, SenderName, DateTime.UtcNow));
    }
}
