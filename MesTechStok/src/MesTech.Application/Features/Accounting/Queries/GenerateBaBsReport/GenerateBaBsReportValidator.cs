using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;

public sealed class GenerateBaBsReportValidator : AbstractValidator<GenerateBaBsReportQuery>
{
    public GenerateBaBsReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
