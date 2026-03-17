using MediatR;

namespace MesTech.Application.Commands.DeleteExpense;

public record DeleteExpenseCommand(Guid Id) : IRequest;
