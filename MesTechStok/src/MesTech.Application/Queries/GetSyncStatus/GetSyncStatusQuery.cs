using MediatR;

namespace MesTech.Application.Queries.GetSyncStatus;

public record GetSyncStatusQuery(string? PlatformCode = null) : IRequest<SyncStatusResult>;

public sealed class SyncStatusResult
{
    public List<PlatformSyncStatus> Platforms { get; set; } = new();
}

public sealed class PlatformSyncStatus
{
    public string PlatformCode { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsConnected { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public int PendingItems { get; set; }
    public int FailedItems { get; set; }
}
