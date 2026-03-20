using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.OrderFulfillmentReport;

/// <summary>
/// Siparis karsilama raporu sorgusu.
/// Belirtilen tarih araliginda platform bazinda gonderi suresi analizi yapar.
/// </summary>
public record OrderFulfillmentReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<IReadOnlyList<OrderFulfillmentReportDto>>;
