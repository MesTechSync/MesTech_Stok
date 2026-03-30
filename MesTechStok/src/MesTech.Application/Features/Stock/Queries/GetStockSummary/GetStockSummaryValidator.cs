using FluentValidation;

namespace MesTech.Application.Features.Stock.Queries.GetStockSummary;

public sealed class GetStockSummaryValidator : AbstractValidator<GetStockSummaryQuery>
{
    public GetStockSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
