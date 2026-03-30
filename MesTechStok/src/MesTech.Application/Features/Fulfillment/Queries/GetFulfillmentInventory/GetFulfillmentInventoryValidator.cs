using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;

public sealed class GetFulfillmentInventoryValidator : AbstractValidator<GetFulfillmentInventoryQuery>
{
    public GetFulfillmentInventoryValidator()
    {
        RuleFor(x => x.Center).IsInEnum();
        RuleFor(x => x.Skus).NotEmpty()
            .WithMessage("En az bir SKU belirtilmelidir.");
    }
}
