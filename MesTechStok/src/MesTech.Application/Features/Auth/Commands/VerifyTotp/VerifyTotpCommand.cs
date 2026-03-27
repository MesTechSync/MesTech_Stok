using MediatR;

namespace MesTech.Application.Features.Auth.Commands.VerifyTotp;

public record VerifyTotpCommand(Guid UserId, string Code) : IRequest<VerifyTotpResult>;

public sealed class VerifyTotpResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
