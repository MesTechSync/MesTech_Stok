using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.UpdateIncome;

public record UpdateIncomeCommand(
    Guid Id,
    string? Description = null,
    decimal? Amount = null,
    IncomeType? IncomeType = null,
    string? Note = null
) : IRequest;
