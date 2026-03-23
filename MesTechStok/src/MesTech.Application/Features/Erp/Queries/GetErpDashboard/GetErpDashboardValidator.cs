using FluentValidation;

namespace MesTech.Application.Features.Erp.Queries.GetErpDashboard;

public class GetErpDashboardValidator : AbstractValidator<GetErpDashboardQuery>
{
    public GetErpDashboardValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
