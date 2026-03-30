using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Queries;

public sealed class CheckVknMukellefValidator : AbstractValidator<CheckVknMukellefQuery>
{
    public CheckVknMukellefValidator()
    {
        RuleFor(x => x.Vkn)
            .NotEmpty().WithMessage("VKN boş olamaz.")
            .Length(10, 11).WithMessage("VKN 10 veya 11 haneli olmalıdır.");
    }
}
