using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;

public class CloseAccountingPeriodValidator : AbstractValidator<CloseAccountingPeriodCommand>
{
    public CloseAccountingPeriodValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
