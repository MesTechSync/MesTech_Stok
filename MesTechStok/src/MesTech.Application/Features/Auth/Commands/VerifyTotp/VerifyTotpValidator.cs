using FluentValidation;

namespace MesTech.Application.Features.Auth.Commands.VerifyTotp;

public sealed class VerifyTotpValidator : AbstractValidator<VerifyTotpCommand>
{
    public VerifyTotpValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli kullanıcı ID gerekli.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("TOTP kodu zorunludur.")
            .Length(6).WithMessage("TOTP kodu 6 haneli olmalı.")
            .Matches(@"^\d{6}$").WithMessage("TOTP kodu sadece rakam içermelidir.");
    }
}
