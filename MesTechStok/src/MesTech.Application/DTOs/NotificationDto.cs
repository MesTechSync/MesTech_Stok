namespace MesTech.Application.DTOs;

/// <summary>
/// Bildirim kaydi DTO.
/// </summary>
public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Sayfalanmis bildirim listesi sonucu.
/// </summary>
public record NotificationListResult(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
