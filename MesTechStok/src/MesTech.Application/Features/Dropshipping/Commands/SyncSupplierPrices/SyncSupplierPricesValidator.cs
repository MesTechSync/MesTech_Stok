using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;

public sealed class SyncSupplierPricesValidator : AbstractValidator<SyncSupplierPricesCommand>
{
    public SyncSupplierPricesValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}
