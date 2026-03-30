using FluentValidation;

namespace MesTech.Application.Features.Stock.Queries.GetStockPlacements;

public sealed class GetStockPlacementsValidator : AbstractValidator<GetStockPlacementsQuery>
{
    public GetStockPlacementsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ShelfCode).MaximumLength(200)
            .When(x => x.ShelfCode is not null);
    }
}
