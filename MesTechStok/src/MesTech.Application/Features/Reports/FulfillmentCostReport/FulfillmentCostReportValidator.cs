using FluentValidation;

namespace MesTech.Application.Features.Reports.FulfillmentCostReport;

public sealed class FulfillmentCostReportValidator : AbstractValidator<FulfillmentCostReportQuery>
{
    public FulfillmentCostReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
