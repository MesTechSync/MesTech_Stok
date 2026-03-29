using FluentValidation;

namespace MesTech.Application.Features.Stock.Commands.CreateStockLot;

public sealed class CreateStockLotValidator : AbstractValidator<CreateStockLotCommand>
{
    public CreateStockLotValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LotNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}
