using FluentValidation;

namespace MesTech.Application.Features.Reports.CommissionReport;

public sealed class CommissionReportValidator : AbstractValidator<CommissionReportQuery>
{
    public CommissionReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
