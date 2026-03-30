using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
