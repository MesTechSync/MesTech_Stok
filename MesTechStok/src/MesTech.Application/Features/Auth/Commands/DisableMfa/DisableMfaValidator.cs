using FluentValidation;

namespace MesTech.Application.Features.Auth.Commands.DisableMfa;

public sealed class DisableMfaValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TotpCode).NotEmpty().Length(6, 8);
    }
}
