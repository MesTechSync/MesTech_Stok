using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;

public record UpdateCounterpartyCommand(
    Guid Id,
    string Name,
    string? VKN = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Platform = null
) : IRequest<bool>;
