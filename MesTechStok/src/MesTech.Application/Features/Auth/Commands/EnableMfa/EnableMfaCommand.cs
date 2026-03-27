using MediatR;

namespace MesTech.Application.Features.Auth.Commands.EnableMfa;

public record EnableMfaCommand(Guid UserId) : IRequest<EnableMfaResult>;

public sealed class EnableMfaResult
{
    public bool IsSuccess { get; init; }
    public string? Secret { get; init; }
    public string? QrCodeUri { get; init; }
    public string? ErrorMessage { get; init; }
}
