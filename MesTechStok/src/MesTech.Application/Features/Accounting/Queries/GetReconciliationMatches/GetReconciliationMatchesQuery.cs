using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;

public record GetReconciliationMatchesQuery(Guid TenantId, ReconciliationStatus? Status = null)
    : IRequest<IReadOnlyList<ReconciliationMatchDto>>;
