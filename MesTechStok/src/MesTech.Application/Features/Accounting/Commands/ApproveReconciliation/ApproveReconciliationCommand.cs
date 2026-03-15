using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;

/// <summary>
/// Mutabakat eslestirme onaylama komutu.
/// NeedsReview durumundaki eslestirmeyi onaylayarak ManualMatch yapar.
/// </summary>
public record ApproveReconciliationCommand(Guid MatchId, Guid ReviewedBy) : IRequest<Unit>;
