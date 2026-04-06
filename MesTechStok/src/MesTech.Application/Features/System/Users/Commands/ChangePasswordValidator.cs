using FluentValidation;

namespace MesTech.Application.Features.System.Users.Commands;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("UserId boş olamaz.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre zorunludur.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter.")
            .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermeli.")
            .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermeli.")
            .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermeli.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Yeni şifre mevcut şifreden farklı olmalı.");
    }
}
