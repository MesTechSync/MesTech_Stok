using FluentValidation;

namespace MesTech.Application.Features.Billing.Queries.GetTenantSubscription;

public sealed class GetTenantSubscriptionValidator : AbstractValidator<GetTenantSubscriptionQuery>
{
    public GetTenantSubscriptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
