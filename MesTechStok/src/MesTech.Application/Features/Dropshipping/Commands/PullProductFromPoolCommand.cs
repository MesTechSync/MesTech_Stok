using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record PullProductFromPoolCommand(
    Guid PoolProductId,
    Guid TargetWarehouseId
) : IRequest<bool>;

public class PullProductFromPoolCommandValidator : AbstractValidator<PullProductFromPoolCommand>
{
    public PullProductFromPoolCommandValidator()
    {
        RuleFor(x => x.PoolProductId).NotEmpty();
        RuleFor(x => x.TargetWarehouseId).NotEmpty();
    }
}

public class PullProductFromPoolCommandHandler(
    IDropshippingPoolRepository poolRepo,
    IStockMovementRepository movementRepo,
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<PullProductFromPoolCommand, bool>
{
    public async Task<bool> Handle(
        PullProductFromPoolCommand req, CancellationToken cancellationToken)
    {
        var poolProduct = await poolRepo.GetPoolProductByIdAsync(req.PoolProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"DropshippingPoolProduct '{req.PoolProductId}' bulunamadı.");

        if (!poolProduct.IsActive)
            throw new InvalidOperationException("Ürün mevcut değil veya pasif durumda.");

        // Stok hareketi oluştur
        var product = await productRepo.GetByIdAsync(poolProduct.ProductId)
            ?? throw new KeyNotFoundException($"Product '{poolProduct.ProductId}' bulunamadı.");

        var previousStock = product.Stock;
        product.AdjustStock(1, StockMovementType.StockIn);

        var movement = new StockMovement
        {
            ProductId = poolProduct.ProductId,
            ToWarehouseId = req.TargetWarehouseId,
            Quantity = 1,
            DocumentNumber = $"POOL-PULL:{poolProduct.Id}",
            Reason = "Havuzdan çekildi",
            Date = DateTime.UtcNow,
            CreatedBy = currentUser.UserId?.ToString() ?? "system"
        };
        movement.SetStockLevels(previousStock, product.Stock);
        movement.SetMovementType(StockMovementType.StockIn);

        await movementRepo.AddAsync(movement);
        await productRepo.UpdateAsync(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
