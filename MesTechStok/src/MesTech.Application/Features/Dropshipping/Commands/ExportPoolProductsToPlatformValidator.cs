using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class ExportPoolProductsToPlatformValidator : AbstractValidator<ExportPoolProductsToPlatformCommand>
{
    public ExportPoolProductsToPlatformValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
    }
}
