using FluentValidation;

namespace MesTech.Application.Commands.PlaceOrder;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).MaximumLength(500).When(x => x.CustomerName != null);
        RuleFor(x => x.CustomerEmail).MaximumLength(500).When(x => x.CustomerEmail != null);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
    }
}
