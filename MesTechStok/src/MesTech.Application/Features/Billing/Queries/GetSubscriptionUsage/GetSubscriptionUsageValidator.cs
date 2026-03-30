using FluentValidation;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;

public sealed class GetSubscriptionUsageValidator : AbstractValidator<GetSubscriptionUsageQuery>
{
    public GetSubscriptionUsageValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
