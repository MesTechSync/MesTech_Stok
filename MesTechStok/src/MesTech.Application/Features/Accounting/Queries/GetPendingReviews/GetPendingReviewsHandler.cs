using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Features.Accounting.Queries.GetPendingReviews;

/// <summary>
/// Inceleme bekleyen mutabakat eslestirme isleyicisi.
/// NeedsReview durumundaki eslestirmeleri, sayfalanmis ve Confidence azalan sirada getirir.
/// </summary>
public class GetPendingReviewsHandler : IRequestHandler<GetPendingReviewsQuery, PendingReviewsResult>
{
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly IBankTransactionRepository _bankTxRepo;

    public GetPendingReviewsHandler(
        IReconciliationMatchRepository matchRepo,
        ISettlementBatchRepository settlementRepo,
        IBankTransactionRepository bankTxRepo)
    {
        _matchRepo = matchRepo;
        _settlementRepo = settlementRepo;
        _bankTxRepo = bankTxRepo;
    }

    public async Task<PendingReviewsResult> Handle(
        GetPendingReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var (matches, totalCount) = await _matchRepo.GetPendingReviewsPagedAsync(
            request.TenantId, request.Page, request.PageSize, cancellationToken);

        var items = new List<PendingReviewMatchDto>();

        foreach (var match in matches)
        {
            var dto = await EnrichMatchAsync(match, cancellationToken);
            items.Add(dto);
        }

        var totalPages = totalCount > 0
            ? (int)Math.Ceiling((double)totalCount / request.PageSize)
            : 0;

        return new PendingReviewsResult
        {
            Items = items.AsReadOnly(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }

    private async Task<PendingReviewMatchDto> EnrichMatchAsync(
        ReconciliationMatch match,
        CancellationToken cancellationToken)
    {
        var dto = new PendingReviewMatchDto
        {
            MatchId = match.Id,
            Confidence = match.Confidence,
            MatchDate = match.MatchDate,
            SettlementBatchId = match.SettlementBatchId,
            BankTransactionId = match.BankTransactionId
        };

        if (match.SettlementBatchId.HasValue)
        {
            var batch = await _settlementRepo.GetByIdAsync(match.SettlementBatchId.Value, cancellationToken);
            if (batch != null)
            {
                dto = dto with
                {
                    SettlementPlatform = batch.Platform,
                    SettlementPeriodStart = batch.PeriodStart,
                    SettlementPeriodEnd = batch.PeriodEnd,
                    SettlementTotalNet = batch.TotalNet
                };
            }
        }

        if (match.BankTransactionId.HasValue)
        {
            var tx = await _bankTxRepo.GetByIdAsync(match.BankTransactionId.Value, cancellationToken);
            if (tx != null)
            {
                dto = dto with
                {
                    BankTransactionDate = tx.TransactionDate,
                    BankTransactionAmount = tx.Amount,
                    BankTransactionDescription = tx.Description
                };
            }
        }

        return dto;
    }
}
