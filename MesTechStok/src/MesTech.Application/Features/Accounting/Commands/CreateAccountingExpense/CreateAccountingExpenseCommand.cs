using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;

public record CreateAccountingExpenseCommand(
    Guid TenantId,
    string Title,
    decimal Amount,
    DateTime ExpenseDate,
    ExpenseSource Source,
    string? Category = null
) : IRequest<Guid>;
