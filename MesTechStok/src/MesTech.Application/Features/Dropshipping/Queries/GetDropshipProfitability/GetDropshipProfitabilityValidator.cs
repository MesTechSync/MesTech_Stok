using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;

public sealed class GetDropshipProfitabilityValidator : AbstractValidator<GetDropshipProfitabilityQuery>
{
    public GetDropshipProfitabilityValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
