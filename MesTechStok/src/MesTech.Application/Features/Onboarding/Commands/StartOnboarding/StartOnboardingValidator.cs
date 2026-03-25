using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Commands.StartOnboarding;

public sealed class StartOnboardingValidator : AbstractValidator<StartOnboardingCommand>
{
    public StartOnboardingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
