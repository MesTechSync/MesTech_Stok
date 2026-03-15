namespace MesTech.Domain.Services;

/// <summary>
/// Buybox analiz girdi modeli.
/// </summary>
public record BuyboxInput(
    string SKU,
    decimal OwnPrice,
    IReadOnlyList<CompetitorPrice> CompetitorPrices);
