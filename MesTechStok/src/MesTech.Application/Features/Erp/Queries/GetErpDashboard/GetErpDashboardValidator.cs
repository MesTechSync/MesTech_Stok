using FluentValidation;

namespace MesTech.Application.Features.Erp.Queries.GetErpDashboard;

public sealed class GetErpDashboardValidator : AbstractValidator<GetErpDashboardQuery>
{
    public GetErpDashboardValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
