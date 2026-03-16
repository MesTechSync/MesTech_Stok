using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.CargoPerformanceReport;

/// <summary>
/// Kargo saglayici performans raporu sorgusu.
/// Belirtilen tarih araliginda gonderi sayisi, ortalama teslimat suresi, maliyet ve basari orani dondurur.
/// </summary>
public record CargoPerformanceReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<IReadOnlyList<CargoPerformanceReportDto>>;
