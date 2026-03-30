using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;

public sealed class GetAccountingPeriodsValidator : AbstractValidator<GetAccountingPeriodsQuery>
{
    public GetAccountingPeriodsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099)
            .When(x => x.Year.HasValue);
    }
}
