using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class ShareProductToPoolValidator : AbstractValidator<ShareProductToPoolCommand>
{
    public ShareProductToPoolValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.TargetPoolId).NotEmpty();
        RuleFor(x => x.PoolPrice).GreaterThanOrEqualTo(0);
    }
}
