using FluentValidation;

namespace MesTech.Application.Features.Reports.CargoPerformanceReport;

public sealed class CargoPerformanceReportValidator : AbstractValidator<CargoPerformanceReportQuery>
{
    public CargoPerformanceReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
