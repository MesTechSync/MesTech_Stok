using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class RemoveProductFromPoolValidator : AbstractValidator<RemoveProductFromPoolCommand>
{
    public RemoveProductFromPoolValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
    }
}
