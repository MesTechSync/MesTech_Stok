using FluentValidation;

namespace MesTech.Application.Commands.AddStock;

public sealed class AddStockValidator : AbstractValidator<AddStockCommand>
{
    public AddStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BatchNumber).MaximumLength(500).When(x => x.BatchNumber != null);
        RuleFor(x => x.DocumentNumber).MaximumLength(500).When(x => x.DocumentNumber != null);
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
    }
}
