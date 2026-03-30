using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;

public sealed class GetShipmentCostsValidator : AbstractValidator<GetShipmentCostsQuery>
{
    public GetShipmentCostsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
