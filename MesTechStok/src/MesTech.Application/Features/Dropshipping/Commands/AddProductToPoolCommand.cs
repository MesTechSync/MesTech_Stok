using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record AddProductToPoolCommand(
    Guid PoolId,
    Guid ProductId,
    Guid? AddedFromFeedId,
    decimal PoolPrice
) : IRequest<Guid>;

public class AddProductToPoolCommandValidator : AbstractValidator<AddProductToPoolCommand>
{
    public AddProductToPoolCommandValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PoolPrice).GreaterThanOrEqualTo(0);
    }
}

public class AddProductToPoolCommandHandler(
    IDropshippingPoolRepository poolRepo,
    IProductRepository productRepo,
    ITenantProvider tenantProvider,
    ICurrentUserService currentUser
) : IRequestHandler<AddProductToPoolCommand, Guid>
{
    public async Task<Guid> Handle(
        AddProductToPoolCommand req, CancellationToken ct)
    {
        var pool = await poolRepo.GetByIdAsync(req.PoolId, ct)
            ?? throw new KeyNotFoundException($"DropshippingPool '{req.PoolId}' bulunamadı.");

        var product = await productRepo.GetByIdAsync(req.ProductId)
            ?? throw new KeyNotFoundException($"Product '{req.ProductId}' bulunamadı.");

        var poolProduct = new DropshippingPoolProduct(
            tenantId: tenantProvider.GetCurrentTenantId(),
            poolId: req.PoolId,
            productId: req.ProductId,
            poolPrice: req.PoolPrice,
            addedFromFeedId: req.AddedFromFeedId
        )
        {
            CreatedBy = currentUser.UserId?.ToString() ?? "system"
        };

        await poolRepo.AddPoolProductAsync(poolProduct, ct);
        return poolProduct.Id;
    }
}
