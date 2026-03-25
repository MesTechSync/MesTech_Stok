namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;

/// <summary>
/// Kullanici ici bildirim DTO.
/// </summary>
public sealed class UserNotificationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Sayfalanmis kullanici ici bildirim listesi sonucu.
/// </summary>
public record UserNotificationListResult(
    IReadOnlyList<UserNotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
