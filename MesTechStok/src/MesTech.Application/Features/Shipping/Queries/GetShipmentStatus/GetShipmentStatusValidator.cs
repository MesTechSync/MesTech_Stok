using FluentValidation;

namespace MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;

public sealed class GetShipmentStatusValidator : AbstractValidator<GetShipmentStatusQuery>
{
    public GetShipmentStatusValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TrackingNumber).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Provider).IsInEnum();
    }
}
