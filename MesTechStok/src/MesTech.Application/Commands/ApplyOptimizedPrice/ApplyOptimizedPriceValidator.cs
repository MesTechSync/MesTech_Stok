using FluentValidation;

namespace MesTech.Application.Commands.ApplyOptimizedPrice;

public class ApplyOptimizedPriceValidator : AbstractValidator<ApplyOptimizedPriceCommand>
{
    public ApplyOptimizedPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
