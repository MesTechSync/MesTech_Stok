using FluentValidation;

namespace MesTech.Application.Features.System.LaunchReadiness;

public sealed class GetLaunchReadinessValidator : AbstractValidator<GetLaunchReadinessQuery>
{
    public GetLaunchReadinessValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
