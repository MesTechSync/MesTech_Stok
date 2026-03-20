using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.TaxSummaryReport;

/// <summary>
/// Vergi beyanname ozet raporu sorgusu.
/// Belirtilen tarih araliginda KDV hesaplanan/indirilecek tutarlarini donem bazinda ozetler.
/// Muhasebe modulundeki GetTaxSummary'den farki: bu rapor beyanname hazirligi icin satis/alis bazli KDV ozeti sunar.
/// </summary>
public record TaxSummaryReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<IReadOnlyList<TaxSummaryReportDto>>;
