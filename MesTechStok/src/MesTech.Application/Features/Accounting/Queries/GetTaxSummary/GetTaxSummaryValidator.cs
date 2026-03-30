using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxSummary;

public sealed class GetTaxSummaryValidator : AbstractValidator<GetTaxSummaryQuery>
{
    public GetTaxSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
    }
}
