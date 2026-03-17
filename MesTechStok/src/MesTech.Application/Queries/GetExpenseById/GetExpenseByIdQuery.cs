using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Queries.GetExpenseById;

public record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseDto?>;
