using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;

/// <summary>
/// Aylik ozet raporu sorgulama — satis, komisyon, gider, vergi metrikleri.
/// </summary>
public record GetMonthlySummaryQuery(int Year, int Month, Guid TenantId)
    : IRequest<MonthlySummaryDto>;
