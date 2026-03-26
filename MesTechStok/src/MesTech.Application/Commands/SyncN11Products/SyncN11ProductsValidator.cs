using FluentValidation;

namespace MesTech.Application.Commands.SyncN11Products;

public sealed class SyncN11ProductsValidator : AbstractValidator<SyncN11ProductsCommand>
{
    public SyncN11ProductsValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
