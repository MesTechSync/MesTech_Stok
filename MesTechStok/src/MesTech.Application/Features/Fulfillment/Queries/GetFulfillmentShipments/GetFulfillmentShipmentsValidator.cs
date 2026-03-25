using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;

public sealed class GetFulfillmentShipmentsValidator : AbstractValidator<GetFulfillmentShipmentsQuery>
{
    public GetFulfillmentShipmentsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
