using MediatR;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

/// <summary>
/// Toplu otomatik fiyat rekabet komutu — tenant'ın tüm ürünlerini (veya platform bazlı)
/// Buybox analizi yapıp fiyat günceller.
/// Endpoint: POST /api/v1/products/auto-compete/bulk
/// Güvenlik: FloorMarginPercent ile maliyet altına inmez.
/// </summary>
public record BulkAutoCompetePriceCommand(
    Guid TenantId,
    string? PlatformCode,
    decimal FloorMarginPercent = 5m,
    decimal MaxDiscountPercent = 5m) : IRequest<BulkAutoCompetePriceResult>;

public sealed class BulkAutoCompetePriceResult
{
    public int TotalProcessed { get; init; }
    public int PriceChanged { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<AutoCompetePriceResult> Details { get; init; } = Array.Empty<AutoCompetePriceResult>();
}
