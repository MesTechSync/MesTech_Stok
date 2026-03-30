using FluentValidation;

namespace MesTech.Application.Features.Reports.PlatformSalesReport;

public sealed class PlatformSalesReportValidator : AbstractValidator<PlatformSalesReportQuery>
{
    public PlatformSalesReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
        RuleFor(x => x.PlatformFilter).MaximumLength(50).When(x => x.PlatformFilter != null);
    }
}
