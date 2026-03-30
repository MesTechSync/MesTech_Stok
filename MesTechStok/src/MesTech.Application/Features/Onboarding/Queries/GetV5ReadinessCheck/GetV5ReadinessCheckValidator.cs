using FluentValidation;

namespace MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;

public sealed class GetV5ReadinessCheckValidator : AbstractValidator<GetV5ReadinessCheckQuery>
{
    public GetV5ReadinessCheckValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
