using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

public class GetIncomeExpenseSummaryValidator : AbstractValidator<GetIncomeExpenseSummaryQuery>
{
    public GetIncomeExpenseSummaryValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
