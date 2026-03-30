using FluentValidation;

namespace MesTech.Application.Features.Reports.CustomerSegmentReport;

public sealed class CustomerSegmentReportValidator : AbstractValidator<CustomerSegmentReportQuery>
{
    public CustomerSegmentReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
