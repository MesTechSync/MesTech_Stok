using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockLots;

/// <summary>
/// Stok lot listesi sorgusu — FIFO lot takibi.
/// G415: StockLot handler for Avalonia StockLotAvaloniaView.
/// </summary>
public record GetStockLotsQuery(Guid TenantId, int Limit = 50)
    : IRequest<IReadOnlyList<StockLotDto>>, ICacheableQuery
{
    public string CacheKey => $"StockLots_{TenantId}_{Limit}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

public record StockLotDto
{
    public Guid Id { get; init; }
    public string LotNumber { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? ProductSku { get; init; }
    public int Quantity { get; init; }
    public int RemainingQuantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? SupplierName { get; init; }
    public string? WarehouseName { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public DateTime ReceivedAt { get; init; }
    public bool IsExpired { get; init; }
    public bool IsFullyConsumed { get; init; }
}
