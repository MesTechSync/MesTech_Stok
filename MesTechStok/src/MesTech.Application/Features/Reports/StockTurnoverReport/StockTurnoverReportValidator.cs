using FluentValidation;

namespace MesTech.Application.Features.Reports.StockTurnoverReport;

public sealed class StockTurnoverReportValidator : AbstractValidator<StockTurnoverReportQuery>
{
    public StockTurnoverReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
