using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

public sealed class BulkAutoCompetePriceValidator : AbstractValidator<BulkAutoCompetePriceCommand>
{
    public BulkAutoCompetePriceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PlatformCode).MaximumLength(50)
            .When(x => x.PlatformCode is not null);
        RuleFor(x => x.FloorMarginPercent).InclusiveBetween(1m, 50m)
            .WithMessage("FloorMarginPercent %1 ile %50 arasında olmalı — maliyet koruması");
        RuleFor(x => x.MaxDiscountPercent).InclusiveBetween(0.1m, 30m)
            .WithMessage("MaxDiscountPercent %0.1 ile %30 arasında olmalı");
    }
}
