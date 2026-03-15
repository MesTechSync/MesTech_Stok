using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.RejectReconciliation;

/// <summary>
/// Mutabakat eslestirme reddetme komutu.
/// NeedsReview durumundaki eslestirmeyi reddeder.
/// </summary>
public record RejectReconciliationCommand(Guid MatchId, Guid ReviewedBy, string? Reason = null) : IRequest<Unit>;
