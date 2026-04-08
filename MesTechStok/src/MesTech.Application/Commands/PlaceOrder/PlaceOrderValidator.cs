using FluentValidation;

namespace MesTech.Application.Commands.PlaceOrder;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).MaximumLength(500).When(x => x.CustomerName != null);
        RuleFor(x => x.CustomerEmail).MaximumLength(500).When(x => x.CustomerEmail != null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Order items cannot be null.")
            .Must(items => items != null && items.Count > 0)
            .WithMessage("Order must have at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty()
                .WithMessage("Product ID is required for each order item.");
            item.RuleFor(i => i.Quantity).GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.UnitPrice).GreaterThan(0)
                .WithMessage("Birim fiyat sıfırdan büyük olmalıdır.");
            item.RuleFor(i => i.TaxRate).InclusiveBetween(0, 1)
                .WithMessage("Tax rate must be between 0 and 1 (e.g. 0.18 for 18%).");
        });
    }
}
