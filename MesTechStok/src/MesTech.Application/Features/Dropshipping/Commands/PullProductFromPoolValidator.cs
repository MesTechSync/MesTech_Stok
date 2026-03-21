using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class PullProductFromPoolValidator : AbstractValidator<PullProductFromPoolCommand>
{
    public PullProductFromPoolValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
        RuleFor(x => x.TargetWarehouseId).NotEmpty();
    }
}
