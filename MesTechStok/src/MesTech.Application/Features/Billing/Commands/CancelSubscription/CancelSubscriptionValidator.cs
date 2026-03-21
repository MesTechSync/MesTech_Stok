using FluentValidation;

namespace MesTech.Application.Features.Billing.Commands.CancelSubscription;

public class CancelSubscriptionValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
    }
}
