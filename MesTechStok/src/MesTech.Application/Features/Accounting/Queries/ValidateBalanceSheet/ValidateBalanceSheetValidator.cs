using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;

public sealed class ValidateBalanceSheetValidator : AbstractValidator<ValidateBalanceSheetQuery>
{
    public ValidateBalanceSheetValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
