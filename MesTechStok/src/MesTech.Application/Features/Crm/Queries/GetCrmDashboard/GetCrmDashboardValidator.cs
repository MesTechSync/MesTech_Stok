using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

public sealed class GetCrmDashboardValidator : AbstractValidator<GetCrmDashboardQuery>
{
    public GetCrmDashboardValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
