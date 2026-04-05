using FluentValidation;

namespace MesTech.Application.Commands.AddStockLot;

public sealed class AddStockLotValidator : AbstractValidator<AddStockLotCommand>
{
    public AddStockLotValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LotNumber).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}
