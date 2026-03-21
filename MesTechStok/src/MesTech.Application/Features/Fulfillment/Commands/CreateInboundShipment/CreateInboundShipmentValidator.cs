using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;

public class CreateInboundShipmentValidator : AbstractValidator<CreateInboundShipmentCommand>
{
    public CreateInboundShipmentValidator()
    {
        RuleFor(x => x.ShipmentName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
    }
}
