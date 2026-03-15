namespace MesTech.Domain.Services;

/// <summary>
/// Rakip fiyat bilgisi.
/// </summary>
public record CompetitorPrice(
    string CompetitorName,
    decimal Price,
    bool IsInStock = true);
