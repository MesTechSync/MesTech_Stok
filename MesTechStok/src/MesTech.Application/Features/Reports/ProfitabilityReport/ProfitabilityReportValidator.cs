using FluentValidation;

namespace MesTech.Application.Features.Reports.ProfitabilityReport;

public sealed class ProfitabilityReportValidator : AbstractValidator<ProfitabilityReportQuery>
{
    public ProfitabilityReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThan(x => x.FromDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
