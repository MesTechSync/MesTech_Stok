using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.ExportProducts;

public sealed class ExportProductsValidator : AbstractValidator<ExportProductsCommand>
{
    public ExportProductsValidator()
    {
        RuleFor(x => x.Format).NotEmpty().MaximumLength(500);
    }
}
