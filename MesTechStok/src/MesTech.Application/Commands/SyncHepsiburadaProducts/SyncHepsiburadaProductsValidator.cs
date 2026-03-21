using FluentValidation;

namespace MesTech.Application.Commands.SyncHepsiburadaProducts;

public class SyncHepsiburadaProductsValidator : AbstractValidator<SyncHepsiburadaProductsCommand>
{
    public SyncHepsiburadaProductsValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
