using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;

public record GetFixedExpensesQuery(
    Guid TenantId,
    bool? IsActive = null
) : IRequest<IReadOnlyList<FixedExpenseDto>>;
