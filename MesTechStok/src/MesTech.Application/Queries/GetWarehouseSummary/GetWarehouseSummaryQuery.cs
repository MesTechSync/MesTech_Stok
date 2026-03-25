using MediatR;

namespace MesTech.Application.Queries.GetWarehouseSummary;

public record GetWarehouseSummaryQuery(Guid TenantId) : IRequest<IReadOnlyList<WarehouseSummaryDto>>;

public sealed class WarehouseSummaryDto
{
    public Guid WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public int OutOfStockCount { get; set; }
    public int CriticalStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int NormalStockCount { get; set; }
    public decimal? MaxCapacity { get; set; }
    public int CapacityPercent { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}
