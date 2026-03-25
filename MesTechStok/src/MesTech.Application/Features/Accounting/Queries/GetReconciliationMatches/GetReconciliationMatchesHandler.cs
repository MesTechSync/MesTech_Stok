using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;

public sealed class GetReconciliationMatchesHandler : IRequestHandler<GetReconciliationMatchesQuery, IReadOnlyList<ReconciliationMatchDto>>
{
    private readonly IReconciliationMatchRepository _repository;

    public GetReconciliationMatchesHandler(IReconciliationMatchRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<ReconciliationMatchDto>> Handle(GetReconciliationMatchesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var status = request.Status ?? ReconciliationStatus.NeedsReview;
        var matches = await _repository.GetByStatusAsync(request.TenantId, status, cancellationToken);
        return matches.Select(m => new ReconciliationMatchDto
        {
            Id = m.Id,
            SettlementBatchId = m.SettlementBatchId,
            BankTransactionId = m.BankTransactionId,
            MatchDate = m.MatchDate,
            Confidence = m.Confidence,
            Status = m.Status.ToString(),
            ReviewedBy = m.ReviewedBy,
            ReviewedAt = m.ReviewedAt
        }).ToList().AsReadOnly();
    }
}
