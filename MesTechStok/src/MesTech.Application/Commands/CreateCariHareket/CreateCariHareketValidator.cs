using FluentValidation;

namespace MesTech.Application.Commands.CreateCariHareket;

public sealed class CreateCariHareketValidator : AbstractValidator<CreateCariHareketCommand>
{
    public CreateCariHareketValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CariHesapId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
