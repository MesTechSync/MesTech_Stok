using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetCounterparties;

public record GetCounterpartiesQuery(Guid TenantId, CounterpartyType? Type = null, bool? IsActive = true)
    : IRequest<IReadOnlyList<CounterpartyDto>>;
