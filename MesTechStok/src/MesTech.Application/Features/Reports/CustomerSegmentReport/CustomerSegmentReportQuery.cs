using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.CustomerSegmentReport;

/// <summary>
/// Musteri segment raporu sorgusu.
/// Belirtilen tarih araliginda musterileri siparis sikligi/tutarina gore segmentleyerek
/// VIP, Regular, New, Dormant gruplarina ayirir.
/// </summary>
public record CustomerSegmentReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<IReadOnlyList<CustomerSegmentReportDto>>;
