using FluentValidation;

namespace MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;

public sealed class GetShipmentLabelValidator : AbstractValidator<GetShipmentLabelQuery>
{
    public GetShipmentLabelValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ShipmentId).NotEmpty().MaximumLength(200);
    }
}
