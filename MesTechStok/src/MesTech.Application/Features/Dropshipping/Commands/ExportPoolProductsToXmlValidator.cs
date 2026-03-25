using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class ExportPoolProductsToXmlValidator : AbstractValidator<ExportPoolProductsToXmlCommand>
{
    public ExportPoolProductsToXmlValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
    }
}
