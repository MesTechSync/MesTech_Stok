using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;

namespace MesTech.Application.Features.Dashboard.Queries.GetAppHubData;

/// <summary>
/// AppHub aggregator — KPI + service health + alerts tek response.
/// G207-DEV6: Dashboard aggregator endpoint eksikliği kapatılıyor.
/// </summary>
public record GetAppHubDataQuery(Guid TenantId) : IRequest<AppHubDataDto>;

public sealed class AppHubDataDto
{
    public int TotalProducts { get; init; }
    public int TotalOrders { get; init; }
    public decimal InventoryValue { get; init; }
    public int LowStockCount { get; init; }
    public int PendingInvoices { get; init; }
    public IReadOnlyList<ServiceHealthDto> ServiceHealth { get; init; } = Array.Empty<ServiceHealthDto>();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
