using FluentValidation;

namespace MesTech.Application.Features.Billing.Queries.GetUserFeatures;

public sealed class GetUserFeaturesValidator : AbstractValidator<GetUserFeaturesQuery>
{
    public GetUserFeaturesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
