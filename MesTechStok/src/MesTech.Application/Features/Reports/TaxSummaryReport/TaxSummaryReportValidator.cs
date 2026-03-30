using FluentValidation;

namespace MesTech.Application.Features.Reports.TaxSummaryReport;

public sealed class TaxSummaryReportValidator : AbstractValidator<TaxSummaryReportQuery>
{
    public TaxSummaryReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
