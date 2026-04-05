using FluentValidation;

namespace MesTech.Application.Commands.DeleteCariHesap;

public sealed class DeleteCariHesapValidator : AbstractValidator<DeleteCariHesapCommand>
{
    public DeleteCariHesapValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("CariHesap ID boş olamaz.");
    }
}
