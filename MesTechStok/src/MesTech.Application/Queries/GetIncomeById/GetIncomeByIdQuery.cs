using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Queries.GetIncomeById;

public record GetIncomeByIdQuery(Guid Id) : IRequest<IncomeDto?>;
