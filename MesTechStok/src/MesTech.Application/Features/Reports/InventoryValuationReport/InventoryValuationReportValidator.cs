using FluentValidation;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

public sealed class InventoryValuationReportValidator : AbstractValidator<InventoryValuationReportQuery>
{
    public InventoryValuationReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
