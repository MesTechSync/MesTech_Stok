using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;

public class GetCashFlowTrendValidator : AbstractValidator<GetCashFlowTrendQuery>
{
    public GetCashFlowTrendValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Months).InclusiveBetween(1, 24);
    }
}
