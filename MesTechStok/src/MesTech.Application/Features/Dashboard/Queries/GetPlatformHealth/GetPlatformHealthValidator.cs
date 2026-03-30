using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;

public sealed class GetPlatformHealthValidator : AbstractValidator<GetPlatformHealthQuery>
{
    public GetPlatformHealthValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
