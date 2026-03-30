using FluentValidation;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;

public sealed class GetSubscriptionPlansValidator : AbstractValidator<GetSubscriptionPlansQuery>
{
    public GetSubscriptionPlansValidator()
    {
        // No parameters to validate — parameterless query
    }
}
