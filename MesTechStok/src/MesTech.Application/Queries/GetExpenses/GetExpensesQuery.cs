using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.GetExpenses;

public record GetExpensesQuery(
    DateTime? From = null,
    DateTime? To = null,
    ExpenseType? Type = null,
    Guid? TenantId = null
) : IRequest<IReadOnlyList<ExpenseDto>>;
