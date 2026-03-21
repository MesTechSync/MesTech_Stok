using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Commands.StartOnboarding;

public class StartOnboardingValidator : AbstractValidator<StartOnboardingCommand>
{
    public StartOnboardingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
