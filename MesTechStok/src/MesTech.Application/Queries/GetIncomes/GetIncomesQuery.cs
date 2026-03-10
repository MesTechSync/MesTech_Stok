using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.GetIncomes;

public record GetIncomesQuery(
    DateTime? From = null,
    DateTime? To = null,
    IncomeType? Type = null,
    Guid? TenantId = null
) : IRequest<IReadOnlyList<IncomeDto>>;
