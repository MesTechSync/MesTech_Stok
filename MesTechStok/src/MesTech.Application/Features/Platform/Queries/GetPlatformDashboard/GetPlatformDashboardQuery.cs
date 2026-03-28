using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;

/// <summary>
/// Generic marketplace dashboard query — works for all 15 platform types.
/// Returns KPI metrics: product count, order count, revenue, sync status.
/// G413: 14 marketplace VM handler.
/// </summary>
public record GetPlatformDashboardQuery(
    Guid TenantId,
    PlatformType Platform
) : IRequest<PlatformDashboardDto>;

public sealed class PlatformDashboardDto
{
    public PlatformType Platform { get; init; }
    public bool IsConnected { get; init; }
    public int ProductCount { get; init; }
    public int OrderCount { get; init; }
    public decimal DailyRevenue { get; init; }
    public string SyncStatus { get; init; } = "Bekliyor";
    public DateTime? LastSyncAt { get; init; }
    public List<PlatformRecentOrderDto> RecentOrders { get; init; } = new();
}

public sealed class PlatformRecentOrderDto
{
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
}
