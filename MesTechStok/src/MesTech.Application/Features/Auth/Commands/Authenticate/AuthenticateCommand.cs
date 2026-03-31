using MediatR;

namespace MesTech.Application.Features.Auth.Commands.Authenticate;

public record AuthenticateCommand(
    string Username,
    string Password
) : IRequest<AuthenticateResult>;
