using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class ExportPoolProductsToXmlValidator : AbstractValidator<ExportPoolProductsToXmlCommand>
{
    public ExportPoolProductsToXmlValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
    }
}
