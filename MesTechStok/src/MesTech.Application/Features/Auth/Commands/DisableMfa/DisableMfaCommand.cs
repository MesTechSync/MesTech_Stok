using MediatR;

namespace MesTech.Application.Features.Auth.Commands.DisableMfa;

public record DisableMfaCommand(Guid UserId, string TotpCode) : IRequest<DisableMfaResult>;

public sealed class DisableMfaResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
