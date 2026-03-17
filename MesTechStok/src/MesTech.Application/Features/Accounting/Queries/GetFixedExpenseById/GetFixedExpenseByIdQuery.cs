using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;

public record GetFixedExpenseByIdQuery(Guid Id) : IRequest<FixedExpenseDto?>;
