using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;

public sealed class GetBalanceSheetValidator : AbstractValidator<GetBalanceSheetQuery>
{
    public GetBalanceSheetValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.AsOfDate).NotEmpty();
    }
}
