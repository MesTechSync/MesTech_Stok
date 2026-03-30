using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;

public sealed class GetSalesChartDataValidator : AbstractValidator<GetSalesChartDataQuery>
{
    public GetSalesChartDataValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Days).InclusiveBetween(1, 365);
        RuleFor(x => x.PlatformCode).MaximumLength(200).When(x => x.PlatformCode is not null);
    }
}
