using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;

/// <summary>
/// Mutabakat dashboard sorgusu — eslestirme durum ozeti.
/// </summary>
public record GetReconciliationDashboardQuery(Guid TenantId)
    : IRequest<ReconciliationDashboardDto>;

/// <summary>
/// Mutabakat dashboard sonuc DTO — sayi ve tutar bazinda istatistikler.
/// </summary>
public record ReconciliationDashboardDto
{
    public int AutoMatchedCount { get; init; }
    public int NeedsReviewCount { get; init; }
    public int UnmatchedCount { get; init; }
    public decimal AutoMatchedTotal { get; init; }
    public decimal NeedsReviewTotal { get; init; }
    public decimal UnmatchedTotal { get; init; }
}
