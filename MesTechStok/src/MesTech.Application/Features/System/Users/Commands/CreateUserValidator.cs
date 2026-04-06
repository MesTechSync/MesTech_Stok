using FluentValidation;

namespace MesTech.Application.Features.System.Users.Commands;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId boş olamaz.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(100).WithMessage("Kullanıcı adı en fazla 100 karakter.")
            .Matches(@"^[a-zA-Z0-9._@-]+$").WithMessage("Kullanıcı adı yalnızca harf, rakam, nokta, alt çizgi, @ ve tire içerebilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter.")
            .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermeli.")
            .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermeli.")
            .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermeli.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçersiz e-posta adresi.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter.");
    }
}
