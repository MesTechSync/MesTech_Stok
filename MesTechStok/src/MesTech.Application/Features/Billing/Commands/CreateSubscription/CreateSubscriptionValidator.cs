using FluentValidation;

namespace MesTech.Application.Features.Billing.Commands.CreateSubscription;

public sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Period).IsInEnum();
    }
}
