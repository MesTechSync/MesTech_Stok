using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class UpdatePoolProductStockValidator : AbstractValidator<UpdatePoolProductStockCommand>
{
    public UpdatePoolProductStockValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
    }
}
