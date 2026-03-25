using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;

public sealed class PlaceDropshipOrderValidator : AbstractValidator<PlaceDropshipOrderCommand>
{
    public PlaceDropshipOrderValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SupplierOrderRef).NotEmpty().MaximumLength(500);
    }
}
