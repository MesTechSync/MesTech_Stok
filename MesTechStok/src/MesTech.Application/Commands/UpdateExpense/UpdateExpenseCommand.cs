using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.UpdateExpense;

public record UpdateExpenseCommand(
    Guid Id,
    string? Description = null,
    decimal? Amount = null,
    ExpenseType? ExpenseType = null,
    PaymentStatus? PaymentStatus = null,
    string? Note = null
) : IRequest;
