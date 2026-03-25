using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;

/// <summary>
/// Dusuk stok uyari sorgusu — stok <= minThreshold olan urunler.
/// Cache: 5 dakika (stok değişiminde invalidate edilir).
/// </summary>
public record GetStockAlertsQuery(Guid TenantId)
    : IRequest<IReadOnlyList<StockAlertDto>>, ICacheableQuery
{
    public string CacheKey => $"StockAlerts_{TenantId}";
}

/// <summary>
/// Stok uyari kalem DTO.
/// </summary>
public record StockAlertDto
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinThreshold { get; init; }
    public string? Platform { get; init; }
}
