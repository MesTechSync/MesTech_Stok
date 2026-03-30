using FluentValidation;

namespace MesTech.Application.Features.Reports.ErpReconciliationReport;

public sealed class ErpReconciliationReportValidator : AbstractValidator<ErpReconciliationReportQuery>
{
    public ErpReconciliationReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ErpProvider).IsInEnum();
    }
}
