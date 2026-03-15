using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreateCounterparty;

public record CreateCounterpartyCommand(
    Guid TenantId,
    string Name,
    CounterpartyType CounterpartyType,
    string? VKN = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Platform = null
) : IRequest<Guid>;
