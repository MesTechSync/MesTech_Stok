using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;

/// <summary>
/// Mutabakat dashboard isleyicisi.
/// ReconciliationMatch tablosundan durum bazinda sayim ve toplam tutar hesaplar.
/// Eslesmemis settlement batch'ler icin de istatistik sunar.
/// </summary>
public sealed class GetReconciliationDashboardHandler
    : IRequestHandler<GetReconciliationDashboardQuery, ReconciliationDashboardDto>
{
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly ISettlementBatchRepository _settlementRepo;

    public GetReconciliationDashboardHandler(
        IReconciliationMatchRepository matchRepo,
        ISettlementBatchRepository settlementRepo)
    {
        _matchRepo = matchRepo;
        _settlementRepo = settlementRepo;
    }

    public async Task<ReconciliationDashboardDto> Handle(
        GetReconciliationDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var autoMatched = await _matchRepo.GetByStatusAsync(
            request.TenantId, ReconciliationStatus.AutoMatched, cancellationToken);
        var manualMatched = await _matchRepo.GetByStatusAsync(
            request.TenantId, ReconciliationStatus.ManualMatch, cancellationToken);
        var needsReview = await _matchRepo.GetByStatusAsync(
            request.TenantId, ReconciliationStatus.NeedsReview, cancellationToken);

        // Get unmatched settlement batches (those without any match record)
        var unmatchedSettlements = await _settlementRepo.GetUnmatchedAsync(
            request.TenantId, cancellationToken);

        // Combine auto and manual matched
        var allAutoMatched = autoMatched.Concat(manualMatched).ToList();

        // Calculate totals by fetching related settlement batches
        var autoMatchedTotal = 0m;
        foreach (var match in allAutoMatched)
        {
            if (match.SettlementBatchId.HasValue)
            {
                var batch = await _settlementRepo.GetByIdAsync(match.SettlementBatchId.Value, cancellationToken);
                if (batch != null) autoMatchedTotal += batch.TotalNet;
            }
        }

        var needsReviewTotal = 0m;
        foreach (var match in needsReview)
        {
            if (match.SettlementBatchId.HasValue)
            {
                var batch = await _settlementRepo.GetByIdAsync(match.SettlementBatchId.Value, cancellationToken);
                if (batch != null) needsReviewTotal += batch.TotalNet;
            }
        }

        var unmatchedTotal = unmatchedSettlements.Sum(s => s.TotalNet);

        return new ReconciliationDashboardDto
        {
            AutoMatchedCount = allAutoMatched.Count,
            NeedsReviewCount = needsReview.Count,
            UnmatchedCount = unmatchedSettlements.Count,
            AutoMatchedTotal = autoMatchedTotal,
            NeedsReviewTotal = needsReviewTotal,
            UnmatchedTotal = unmatchedTotal
        };
    }
}
