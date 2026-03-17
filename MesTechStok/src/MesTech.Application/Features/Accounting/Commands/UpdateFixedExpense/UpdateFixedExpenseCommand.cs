using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;

public record UpdateFixedExpenseCommand(
    Guid Id,
    decimal? MonthlyAmount = null,
    bool? IsActive = null
) : IRequest;
