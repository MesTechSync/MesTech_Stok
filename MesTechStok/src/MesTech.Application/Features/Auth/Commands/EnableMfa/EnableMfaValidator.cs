using FluentValidation;

namespace MesTech.Application.Features.Auth.Commands.EnableMfa;

public sealed class EnableMfaValidator : AbstractValidator<EnableMfaCommand>
{
    public EnableMfaValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli kullanıcı ID gerekli.");
    }
}
