using FluentValidation;

namespace MesTech.Application.Features.Stock.Queries.GetStockTransfers;

public sealed class GetStockTransfersValidator : AbstractValidator<GetStockTransfersQuery>
{
    public GetStockTransfersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
