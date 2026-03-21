using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class ExportPoolProductsToCsvValidator : AbstractValidator<ExportPoolProductsToCsvCommand>
{
    public ExportPoolProductsToCsvValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
    }
}
