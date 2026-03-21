using FluentValidation;

namespace MesTech.Application.Commands.UpdateProductContent;

public class UpdateProductContentValidator : AbstractValidator<UpdateProductContentCommand>
{
    public UpdateProductContentValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(500);
        RuleFor(x => x.GeneratedContent).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AiProvider).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
