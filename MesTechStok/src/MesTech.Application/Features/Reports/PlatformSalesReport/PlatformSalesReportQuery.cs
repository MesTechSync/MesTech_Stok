using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.PlatformSalesReport;

/// <summary>
/// Platform bazli satis raporu sorgusu.
/// Belirtilen tarih araliginda tum platformlarin siparis, gelir, iade, komisyon ve net gelir ozetini dondurur.
/// </summary>
public record PlatformSalesReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string? PlatformFilter = null
) : IRequest<IReadOnlyList<PlatformSalesReportDto>>;
