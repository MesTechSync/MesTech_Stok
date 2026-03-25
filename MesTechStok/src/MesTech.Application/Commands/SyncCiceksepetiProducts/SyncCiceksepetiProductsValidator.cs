using FluentValidation;

namespace MesTech.Application.Commands.SyncCiceksepetiProducts;

public sealed class SyncCiceksepetiProductsValidator : AbstractValidator<SyncCiceksepetiProductsCommand>
{
    public SyncCiceksepetiProductsValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
