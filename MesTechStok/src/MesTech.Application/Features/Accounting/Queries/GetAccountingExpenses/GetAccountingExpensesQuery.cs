using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;

public record GetAccountingExpensesQuery(Guid TenantId, DateTime From, DateTime To, ExpenseSource? Source = null)
    : IRequest<IReadOnlyList<AccountingExpenseDto>>;
