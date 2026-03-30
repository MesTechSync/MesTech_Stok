using FluentValidation;

namespace MesTech.Application.Features.Stock.Queries.GetStockLots;

public sealed class GetStockLotsValidator : AbstractValidator<GetStockLotsQuery>
{
    public GetStockLotsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
