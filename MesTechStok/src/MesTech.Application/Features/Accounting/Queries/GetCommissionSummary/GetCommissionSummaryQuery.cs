using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;

public record GetCommissionSummaryQuery(Guid TenantId, DateTime From, DateTime To)
    : IRequest<CommissionSummaryDto>;
