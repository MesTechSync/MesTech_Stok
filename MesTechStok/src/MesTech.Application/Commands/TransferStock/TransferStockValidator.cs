using FluentValidation;

namespace MesTech.Application.Commands.TransferStock;

public class TransferStockValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.TargetWarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
    }
}
