using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;

public sealed class GetFulfillmentOrdersValidator : AbstractValidator<GetFulfillmentOrdersQuery>
{
    public GetFulfillmentOrdersValidator()
    {
        RuleFor(x => x.Center).IsInEnum();
        RuleFor(x => x.Since).NotEmpty()
            .WithMessage("Baslangic tarihi belirtilmelidir.");
    }
}
