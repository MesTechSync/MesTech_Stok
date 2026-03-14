using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Finance.Commands.CreateExpense;

public record CreateExpenseCommand(
    Guid TenantId, string Title, decimal Amount, ExpenseCategory Category,
    DateTime ExpenseDate, Guid? SubmittedByUserId = null,
    string? Notes = null, Guid? StoreId = null
) : IRequest<Guid>;
