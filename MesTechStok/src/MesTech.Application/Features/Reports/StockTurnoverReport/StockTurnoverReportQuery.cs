using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.StockTurnoverReport;

/// <summary>
/// Stok devir hizi raporu sorgusu.
/// Belirtilen tarih araliginda urun bazinda satis miktari, ortalama stok gunu, devir orani ve stok karsilama gununu dondurur.
/// </summary>
public record StockTurnoverReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CategoryFilter = null
) : IRequest<IReadOnlyList<StockTurnoverReportDto>>;
