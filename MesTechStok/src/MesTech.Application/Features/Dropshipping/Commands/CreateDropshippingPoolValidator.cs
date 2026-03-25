using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class CreateDropshippingPoolValidator : AbstractValidator<CreateDropshippingPoolCommand>
{
    public CreateDropshippingPoolValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
