using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.CreateExpense;

public record CreateExpenseCommand(
    Guid TenantId,
    Guid? StoreId,
    string Description,
    decimal Amount,
    ExpenseType ExpenseType,
    DateTime? Date,
    string? Note,
    bool IsRecurring = false,
    string? RecurrencePeriod = null
) : IRequest<Guid>;
