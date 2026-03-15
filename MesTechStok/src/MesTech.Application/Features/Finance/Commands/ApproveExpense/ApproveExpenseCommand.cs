using MediatR;

namespace MesTech.Application.Features.Finance.Commands.ApproveExpense;

public record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverUserId) : IRequest<Unit>;
