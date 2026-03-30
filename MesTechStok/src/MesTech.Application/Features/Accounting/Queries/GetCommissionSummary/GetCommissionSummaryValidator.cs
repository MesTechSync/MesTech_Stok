using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;

public sealed class GetCommissionSummaryValidator : AbstractValidator<GetCommissionSummaryQuery>
{
    public GetCommissionSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty().GreaterThanOrEqualTo(x => x.From);
    }
}
