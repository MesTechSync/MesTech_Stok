using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record ShareProductToPoolCommand(
    Guid ProductId,
    Guid TargetPoolId,
    decimal PoolPrice,
    Guid? SourceFeedId
) : IRequest<Guid>;

public class ShareProductToPoolCommandValidator : AbstractValidator<ShareProductToPoolCommand>
{
    public ShareProductToPoolCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.TargetPoolId).NotEmpty();
        RuleFor(x => x.PoolPrice).GreaterThanOrEqualTo(0);
    }
}

public class ShareProductToPoolCommandHandler(
    IDropshippingPoolRepository poolRepo,
    IProductRepository productRepo,
    ITenantProvider tenantProvider,
    ICurrentUserService currentUser
) : IRequestHandler<ShareProductToPoolCommand, Guid>
{
    public async Task<Guid> Handle(
        ShareProductToPoolCommand req, CancellationToken cancellationToken)
    {
        var pool = await poolRepo.GetByIdAsync(req.TargetPoolId, cancellationToken)
            ?? throw new KeyNotFoundException($"DropshippingPool '{req.TargetPoolId}' bulunamadı.");

        var product = await productRepo.GetByIdAsync(req.ProductId)
            ?? throw new KeyNotFoundException($"Product '{req.ProductId}' bulunamadı.");

        var poolProduct = new DropshippingPoolProduct(
            tenantId: tenantProvider.GetCurrentTenantId(),
            poolId: req.TargetPoolId,
            productId: req.ProductId,
            poolPrice: req.PoolPrice,
            addedFromFeedId: req.SourceFeedId
        )
        {
            CreatedBy = currentUser.UserId?.ToString() ?? "system"
        };

        await poolRepo.AddPoolProductAsync(poolProduct, cancellationToken);
        return poolProduct.Id;
    }
}
