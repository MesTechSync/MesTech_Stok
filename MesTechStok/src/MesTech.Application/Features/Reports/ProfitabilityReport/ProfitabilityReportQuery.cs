using MediatR;

namespace MesTech.Application.Features.Reports.ProfitabilityReport;

/// <summary>
/// Karlılık raporu sorgusu.
/// Net Kar = Satis Fiyati - Alis Maliyeti(WAC) - Komisyon - Kargo - KDV
/// Turkiye KDV: Satis fiyati KDV DAHIL → KDV = Fiyat * KDVOrani / (1 + KDVOrani)
/// </summary>
public record ProfitabilityReportQuery(
    Guid TenantId,
    DateTime FromDate,
    DateTime ToDate) : IRequest<ProfitabilityReportDto>;

public record ProfitabilityReportDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    // Ozet
    public decimal TotalRevenue { get; init; }
    public decimal TotalCost { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalTax { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ProfitMargin { get; init; }
    public int TotalOrders { get; init; }

    // Platform bazli kirilim
    public IReadOnlyList<PlatformProfitDto> ByPlatform { get; init; } = [];

    // En karli urunler
    public IReadOnlyList<ProductProfitDto> TopProfitableProducts { get; init; } = [];

    // En zararlı urunler
    public IReadOnlyList<ProductProfitDto> LeastProfitableProducts { get; init; } = [];
}

public record PlatformProfitDto(
    string Platform,
    int OrderCount,
    decimal Revenue,
    decimal Cost,
    decimal Commission,
    decimal Tax,
    decimal NetProfit,
    decimal ProfitMargin);

public record ProductProfitDto(
    Guid ProductId,
    string SKU,
    string Name,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal NetProfit,
    decimal ProfitMargin);
