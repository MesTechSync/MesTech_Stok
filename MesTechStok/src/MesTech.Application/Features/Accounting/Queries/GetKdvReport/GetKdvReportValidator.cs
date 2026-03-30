using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvReport;

public sealed class GetKdvReportValidator : AbstractValidator<GetKdvReportQuery>
{
    public GetKdvReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
