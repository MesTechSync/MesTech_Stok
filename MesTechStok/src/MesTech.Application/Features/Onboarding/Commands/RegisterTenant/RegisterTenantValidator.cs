using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Commands.RegisterTenant;

public sealed class RegisterTenantValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Firma adı zorunlu.")
            .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter.");

        RuleFor(x => x.AdminUsername)
            .NotEmpty().WithMessage("Kullanıcı adı zorunlu.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter.")
            .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter.")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Kullanıcı adı sadece harf, rakam, nokta, tire, alt çizgi içerebilir.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("E-posta zorunlu.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Şifre zorunlu.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter.")
            .Matches("[A-Z]").WithMessage("Şifre en az 1 büyük harf içermeli.")
            .Matches("[0-9]").WithMessage("Şifre en az 1 rakam içermeli.");
    }
}
