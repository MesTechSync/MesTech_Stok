using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.CustomerLifetimeValueReport;

/// <summary>
/// Musteri yasam boyu degeri (CLV) raporu sorgusu.
/// Belirtilen tarih araliginda musteri bazinda siparis analizi ve tahmini CLV hesaplar.
/// </summary>
public record CustomerLifetimeValueReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    int MinOrderCount = 1
) : IRequest<IReadOnlyList<CustomerLifetimeValueReportDto>>;
