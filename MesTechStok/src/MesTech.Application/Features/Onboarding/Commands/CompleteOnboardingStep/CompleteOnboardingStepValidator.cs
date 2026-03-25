using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;

public sealed class CompleteOnboardingStepValidator : AbstractValidator<CompleteOnboardingStepCommand>
{
    public CompleteOnboardingStepValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
