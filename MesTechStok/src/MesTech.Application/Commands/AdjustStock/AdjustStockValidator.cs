using FluentValidation;

namespace MesTech.Application.Commands.AdjustStock;

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0)
            .WithMessage("Adjustment quantity cannot be zero.");
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
        RuleFor(x => x.PerformedBy).MaximumLength(500).When(x => x.PerformedBy != null);
    }
}
