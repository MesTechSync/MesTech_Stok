using FluentValidation;

namespace MesTech.Application.Features.Auth.Commands.Authenticate;

public sealed class AuthenticateValidator : AbstractValidator<AuthenticateCommand>
{
    public AuthenticateValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Username is required and must be at most 100 characters.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(256)
            .WithMessage("Password is required.");
    }
}
