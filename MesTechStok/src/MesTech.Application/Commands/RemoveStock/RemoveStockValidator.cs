using FluentValidation;

namespace MesTech.Application.Commands.RemoveStock;

public sealed class RemoveStockValidator : AbstractValidator<RemoveStockCommand>
{
    public RemoveStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
        RuleFor(x => x.DocumentNumber).MaximumLength(500).When(x => x.DocumentNumber != null);
    }
}
