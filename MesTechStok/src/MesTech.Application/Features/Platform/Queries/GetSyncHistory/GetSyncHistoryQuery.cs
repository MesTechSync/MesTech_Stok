using MediatR;

namespace MesTech.Application.Features.Platform.Queries.GetSyncHistory;

/// <summary>
/// Son senkronizasyon geçmişi — Ekran 19 alt tablosu.
/// </summary>
public record GetSyncHistoryQuery(
    Guid TenantId,
    string? PlatformFilter = null,
    int Count = 20
) : IRequest<IReadOnlyList<SyncHistoryItemDto>>;

public sealed class SyncHistoryItemDto
{
    public Guid Id { get; set; }
    public string PlatformCode { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Duration { get; set; }
}
