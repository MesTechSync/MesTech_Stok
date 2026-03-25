using FluentValidation;

namespace MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;

public sealed class ChangeSubscriptionPlanValidator : AbstractValidator<ChangeSubscriptionPlanCommand>
{
    public ChangeSubscriptionPlanValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId zorunlu.");
        RuleFor(x => x.NewPlanId).NotEmpty().WithMessage("NewPlanId zorunlu.");
    }
}
