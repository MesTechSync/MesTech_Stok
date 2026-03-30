using FluentValidation;

namespace MesTech.Application.Features.Reports.CustomerLifetimeValueReport;

public sealed class CustomerLifetimeValueReportValidator : AbstractValidator<CustomerLifetimeValueReportQuery>
{
    public CustomerLifetimeValueReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
        RuleFor(x => x.MinOrderCount).GreaterThanOrEqualTo(1);
    }
}
