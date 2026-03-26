using FluentValidation;

namespace MesTech.Application.Commands.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).MaximumLength(200).EmailAddress().When(x => x.CustomerEmail != null);
        RuleFor(x => x.OrderType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
