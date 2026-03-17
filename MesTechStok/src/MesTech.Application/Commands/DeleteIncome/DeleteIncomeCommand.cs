using MediatR;

namespace MesTech.Application.Commands.DeleteIncome;

public record DeleteIncomeCommand(Guid Id) : IRequest;
