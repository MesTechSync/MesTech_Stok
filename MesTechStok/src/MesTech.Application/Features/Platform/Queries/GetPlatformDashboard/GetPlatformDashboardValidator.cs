using FluentValidation;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;

public sealed class GetPlatformDashboardValidator : AbstractValidator<GetPlatformDashboardQuery>
{
    public GetPlatformDashboardValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
    }
}
