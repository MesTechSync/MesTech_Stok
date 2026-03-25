using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;

public sealed class LinkDropshipProductValidator : AbstractValidator<LinkDropshipProductCommand>
{
    public LinkDropshipProductValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DropshipProductId).NotEmpty();
        RuleFor(x => x.MesTechProductId).NotEmpty();
    }
}
