using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;

public class CompleteOnboardingStepValidator : AbstractValidator<CompleteOnboardingStepCommand>
{
    public CompleteOnboardingStepValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
