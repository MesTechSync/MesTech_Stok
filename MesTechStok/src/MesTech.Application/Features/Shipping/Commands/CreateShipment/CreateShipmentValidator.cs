using MesTech.Domain.Enums;
using FluentValidation;

namespace MesTech.Application.Features.Shipping.Commands.CreateShipment;

public sealed class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CargoProvider).NotEqual(CargoProvider.None)
            .WithMessage("Cargo provider must be specified.");
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RecipientAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RecipientPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Weight).GreaterThan(0).LessThanOrEqualTo(150);
    }
}
