using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

public sealed class AutoCompetePriceValidator : AbstractValidator<AutoCompetePriceCommand>
{
    public AutoCompetePriceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FloorPrice).GreaterThan(0)
            .WithMessage("FloorPrice sıfırdan büyük olmalı — zarara satış koruması");
        RuleFor(x => x.MaxDiscountPercent).InclusiveBetween(0.1m, 30m)
            .WithMessage("MaxDiscountPercent %0.1 ile %30 arasında olmalı");
    }
}
