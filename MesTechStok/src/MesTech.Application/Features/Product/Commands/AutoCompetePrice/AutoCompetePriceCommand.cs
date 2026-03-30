using MediatR;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

/// <summary>
/// Otomatik fiyat rekabet komutu — Buybox analizi yapıp rakiplere göre fiyat günceller.
/// G6877: Sentos'ta var, MesTech'e ekleniyor.
/// Güvenlik: FloorPrice altına asla inmez, MaxDiscountPercent sınırı var.
/// </summary>
public record AutoCompetePriceCommand(
    Guid TenantId,
    Guid ProductId,
    string PlatformCode,
    decimal FloorPrice,
    decimal MaxDiscountPercent = 5m) : IRequest<AutoCompetePriceResult>;

public sealed class AutoCompetePriceResult
{
    public bool IsSuccess { get; init; }
    public bool PriceChanged { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal CompetitorPrice { get; init; }
    public string CompetitorName { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static AutoCompetePriceResult NoChange(decimal currentPrice, string reason)
        => new() { IsSuccess = true, PriceChanged = false, OldPrice = currentPrice, NewPrice = currentPrice, Reasoning = reason };

    public static AutoCompetePriceResult Changed(decimal oldPrice, decimal newPrice, decimal competitorPrice, string competitorName, string reason)
        => new() { IsSuccess = true, PriceChanged = true, OldPrice = oldPrice, NewPrice = newPrice, CompetitorPrice = competitorPrice, CompetitorName = competitorName, Reasoning = reason };

    public static AutoCompetePriceResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
