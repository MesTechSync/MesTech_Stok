using MediatR;

namespace MesTech.Application.Features.Finance.Commands.MarkExpensePaid;

public record MarkExpensePaidCommand(Guid ExpenseId, Guid BankAccountId) : IRequest<Unit>;
