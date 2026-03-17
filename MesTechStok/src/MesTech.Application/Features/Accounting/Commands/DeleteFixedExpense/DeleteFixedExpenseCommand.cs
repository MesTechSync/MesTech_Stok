using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;

public record DeleteFixedExpenseCommand(Guid Id) : IRequest;
