using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class AddProductToPoolValidator : AbstractValidator<AddProductToPoolCommand>
{
    public AddProductToPoolValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PoolPrice).GreaterThanOrEqualTo(0);
    }
}
