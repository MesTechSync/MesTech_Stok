using MediatR;
using MesTech.Application.DTOs.Finance;

namespace MesTech.Application.Features.Finance.Queries.GetBudgetSummary;

public record GetBudgetSummaryQuery(Guid TenantId, int Year, int Month) : IRequest<BudgetSummaryDto>;
