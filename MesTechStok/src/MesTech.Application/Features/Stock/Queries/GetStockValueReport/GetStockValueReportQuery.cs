using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockValueReport;

public record GetStockValueReportQuery(Guid TenantId, Guid? WarehouseId = null)
    : IRequest<StockValueReportResult>, ICacheableQuery
{
    public string CacheKey => $"StockValue_{TenantId}_{WarehouseId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class StockValueReportResult
{
    public decimal TotalValue { get; init; }
    public decimal TotalCostValue { get; init; }
    public decimal UnrealizedProfitLoss { get; init; }
    public int TotalProducts { get; init; }
    public int ZeroStockProducts { get; init; }
    public IReadOnlyList<StockValueLineDto> TopValueProducts { get; init; } = [];
}

public sealed class StockValueLineDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public int Stock { get; init; }
    public decimal Price { get; init; }
    public decimal CostPrice { get; init; }
    public decimal TotalValue { get; init; }
    public decimal TotalCost { get; init; }
}
