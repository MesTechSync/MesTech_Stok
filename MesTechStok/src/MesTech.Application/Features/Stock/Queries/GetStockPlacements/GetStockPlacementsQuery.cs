using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockPlacements;

/// <summary>
/// Stok yerlesim sorgusu — depo/raf bazli urun konumu.
/// G415: StockPlacement handler for Avalonia StockPlacementAvaloniaView.
/// </summary>
public record GetStockPlacementsQuery(Guid TenantId, Guid? WarehouseId = null, string? ShelfCode = null)
    : IRequest<IReadOnlyList<StockPlacementDto>>, ICacheableQuery
{
    public string CacheKey => $"StockPlacements_{TenantId}_{WarehouseId}_{ShelfCode}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record StockPlacementDto
{
    public Guid Id { get; init; }
    public string? ProductName { get; init; }
    public string? ProductSku { get; init; }
    public int Quantity { get; init; }
    public int MinimumStock { get; init; }
    public string? WarehouseName { get; init; }
    public string? ShelfCode { get; init; }
    public string? BinCode { get; init; }
    public string StockStatus { get; init; } = string.Empty;
}
