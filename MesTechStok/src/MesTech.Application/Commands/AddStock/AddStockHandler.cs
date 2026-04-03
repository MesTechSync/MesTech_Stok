using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.AddStock;

public sealed class AddStockHandler : IRequestHandler<AddStockCommand, AddStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public AddStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    public async Task<AddStockResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
            return new AddStockResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        var previousStock = product.Stock;

        // Domain logic — event fırlatır
        product.AdjustStock(request.Quantity, StockMovementType.StockIn);

        // Hareket kaydı
        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            TotalCost = request.Quantity * request.UnitCost,
            BatchNumber = request.BatchNumber,
            ExpiryDate = request.ExpiryDate,
            DocumentNumber = request.DocumentNumber,
            Reason = request.Reason,
            Date = DateTime.UtcNow
        };
        movement.SetStockLevels(previousStock, product.Stock);
        movement.SetMovementType(StockMovementType.StockIn);

        await _movementRepository.AddAsync(movement).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AddStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
