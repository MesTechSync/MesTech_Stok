using FluentValidation;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

public sealed class GetStockValueReportValidator : AbstractValidator<GetStockValueReportQuery>
{
    public GetStockValueReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
