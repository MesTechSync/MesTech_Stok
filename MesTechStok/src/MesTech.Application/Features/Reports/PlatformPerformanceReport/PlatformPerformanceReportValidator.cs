using FluentValidation;

namespace MesTech.Application.Features.Reports.PlatformPerformanceReport;

public sealed class PlatformPerformanceReportValidator : AbstractValidator<PlatformPerformanceReportQuery>
{
    public PlatformPerformanceReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
