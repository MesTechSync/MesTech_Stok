using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

public sealed class GetIncomeExpenseSummaryValidator : AbstractValidator<GetIncomeExpenseSummaryQuery>
{
    public GetIncomeExpenseSummaryValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
