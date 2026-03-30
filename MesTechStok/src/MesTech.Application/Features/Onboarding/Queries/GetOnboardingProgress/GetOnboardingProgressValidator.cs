using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;

public sealed class GetOnboardingProgressValidator : AbstractValidator<GetOnboardingProgressQuery>
{
    public GetOnboardingProgressValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
