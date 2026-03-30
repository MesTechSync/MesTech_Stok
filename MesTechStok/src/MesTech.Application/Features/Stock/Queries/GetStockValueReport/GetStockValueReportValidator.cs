using FluentValidation;

namespace MesTech.Application.Features.Stock.Queries.GetStockValueReport;

public sealed class GetStockValueReportValidator : AbstractValidator<GetStockValueReportQuery>
{
    public GetStockValueReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
