using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;

public sealed class GetReconciliationDashboardValidator : AbstractValidator<GetReconciliationDashboardQuery>
{
    public GetReconciliationDashboardValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
