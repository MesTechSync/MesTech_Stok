namespace MesTech.Application.Interfaces;

/// <summary>
/// S3-DEV3-01: Platform mesaj çekme + yanıtlama interface'i.
/// Trendyol: müşteri soruları (Q&amp;A), HB: mesajlaşma, N11: SOAP QuestionService.
/// </summary>
public interface IMessageCapableAdapter
{
    Task<IReadOnlyList<PlatformMessage>> GetMessagesAsync(DateTime since, int page = 0, int size = 50, CancellationToken ct = default);
    Task<bool> ReplyToMessageAsync(string messageId, string reply, CancellationToken ct = default);
}

/// <summary>
/// Platform mesaj DTO — tüm platformlar için ortak model.
/// </summary>
public sealed class PlatformMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // WAITING_FOR_ANSWER, ANSWERED, etc.
    public DateTime ReceivedAt { get; set; }
    public string? ReplyText { get; set; }
    public DateTime? RepliedAt { get; set; }
}
