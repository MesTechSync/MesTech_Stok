namespace MesTech.Application.DTOs.Crm;

/// <summary>
/// Platform Message data transfer object.
/// </summary>
public sealed class PlatformMessageDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool HasAiSuggestion { get; set; }
    public string? AiSuggestedReply { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string? RepliedBy { get; set; }
}
