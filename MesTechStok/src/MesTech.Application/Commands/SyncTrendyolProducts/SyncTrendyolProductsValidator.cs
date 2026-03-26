using FluentValidation;

namespace MesTech.Application.Commands.SyncTrendyolProducts;

public sealed class SyncTrendyolProductsValidator : AbstractValidator<SyncTrendyolProductsCommand>
{
    public SyncTrendyolProductsValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
