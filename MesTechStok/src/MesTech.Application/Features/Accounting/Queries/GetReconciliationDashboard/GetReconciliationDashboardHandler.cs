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

        // Batch fetch settlement batches — eliminates N+1 query
        var allSettlementIds = allAutoMatched
            .Concat(needsReview)
            .Where(m => m.SettlementBatchId.HasValue)
            .Select(m => m.SettlementBatchId!.Value)
            .Distinct()
            .ToList();

        var settlementBatches = allSettlementIds.Count > 0
            ? (await _settlementRepo.GetByIdsAsync(allSettlementIds, cancellationToken)).ToDictionary(b => b.Id)
            : new Dictionary<Guid, Domain.Accounting.Entities.SettlementBatch>();

        var autoMatchedTotal = allAutoMatched
            .Where(m => m.SettlementBatchId.HasValue && settlementBatches.ContainsKey(m.SettlementBatchId.Value))
            .Sum(m => settlementBatches[m.SettlementBatchId!.Value].TotalNet);

        var needsReviewTotal = needsReview
            .Where(m => m.SettlementBatchId.HasValue && settlementBatches.ContainsKey(m.SettlementBatchId.Value))
            .Sum(m => settlementBatches[m.SettlementBatchId!.Value].TotalNet);

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
