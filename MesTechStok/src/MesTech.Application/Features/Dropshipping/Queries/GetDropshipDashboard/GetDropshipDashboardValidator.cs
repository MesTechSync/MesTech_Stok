using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;

public sealed class GetDropshipDashboardValidator : AbstractValidator<GetDropshipDashboardQuery>
{
    public GetDropshipDashboardValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
