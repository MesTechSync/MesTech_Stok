using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;

public sealed class GetMonthlySummaryValidator : AbstractValidator<GetMonthlySummaryQuery>
{
    public GetMonthlySummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
