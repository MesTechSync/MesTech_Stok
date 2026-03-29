using MediatR;

namespace MesTech.Application.Features.Hr.Commands.CreateTimeEntry;

public record CreateTimeEntryCommand(
    Guid TenantId,
    Guid WorkTaskId,
    Guid UserId,
    string? Description = null,
    bool IsBillable = false,
    decimal? HourlyRate = null
) : IRequest<Guid>;
