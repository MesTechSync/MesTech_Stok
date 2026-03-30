using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;

public sealed class GetRevenueChartValidator : AbstractValidator<GetRevenueChartQuery>
{
    public GetRevenueChartValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Days).InclusiveBetween(1, 365);
    }
}
