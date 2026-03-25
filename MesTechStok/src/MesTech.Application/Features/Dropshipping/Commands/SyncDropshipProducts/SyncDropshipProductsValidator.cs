using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;

public sealed class SyncDropshipProductsValidator : AbstractValidator<SyncDropshipProductsCommand>
{
    public SyncDropshipProductsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}
