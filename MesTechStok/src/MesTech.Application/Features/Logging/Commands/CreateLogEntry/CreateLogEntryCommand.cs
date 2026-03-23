using MediatR;

namespace MesTech.Application.Features.Logging.Commands.CreateLogEntry;

public record CreateLogEntryCommand(
    Guid TenantId,
    string Level,
    string Category,
    string Message,
    string? Data = null,
    string? UserId = null,
    string? Exception = null,
    string? MachineName = null
) : IRequest<Guid>;
