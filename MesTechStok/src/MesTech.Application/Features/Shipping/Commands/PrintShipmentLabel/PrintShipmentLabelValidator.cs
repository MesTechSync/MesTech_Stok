using FluentValidation;

namespace MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;

public sealed class PrintShipmentLabelValidator : AbstractValidator<PrintShipmentLabelCommand>
{
    public PrintShipmentLabelValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.PrinterName)
            .MaximumLength(500)
            .When(x => x.PrinterName is not null);
    }
}
