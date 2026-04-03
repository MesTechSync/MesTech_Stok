using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.TransferStock;

public sealed class TransferStockHandler : IRequestHandler<TransferStockCommand, TransferStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public TransferStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

#pragma warning disable MA0051 // Method is too long — warehouse transfer requires atomic dual-movement creation
    public async Task<TransferStockResult> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validation
        if (request.Quantity <= 0)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = "Transfer miktarı pozitif olmalı." };

        if (request.SourceWarehouseId == request.TargetWarehouseId)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = "Kaynak ve hedef depo aynı olamaz." };

        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        var sourceWarehouse = await _warehouseRepository.GetByIdAsync(request.SourceWarehouseId).ConfigureAwait(false);
        if (sourceWarehouse == null)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = $"Source warehouse {request.SourceWarehouseId} not found." };

        var targetWarehouse = await _warehouseRepository.GetByIdAsync(request.TargetWarehouseId).ConfigureAwait(false);
        if (targetWarehouse == null)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = $"Target warehouse {request.TargetWarehouseId} not found." };

        if (product.Stock < request.Quantity)
            return new TransferStockResult { IsSuccess = false, ErrorMessage = $"Yetersiz stok. Mevcut: {product.Stock}, İstenen: {request.Quantity}" };

        var previousStock = product.Stock;

        // OUT movement from source
        var outMovement = new StockMovement
        {
            ProductId = request.ProductId,
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Quantity = -request.Quantity,
            FromWarehouseId = request.SourceWarehouseId,
            ToWarehouseId = request.TargetWarehouseId,
            Notes = request.Notes,
            Reason = $"Transfer: {sourceWarehouse.Name} → {targetWarehouse.Name}",
            Date = DateTime.UtcNow
        };
        outMovement.SetStockLevels(previousStock, previousStock - request.Quantity);
        outMovement.SetMovementType(StockMovementType.Transfer);

        // IN movement to target
        var inMovement = new StockMovement
        {
            ProductId = request.ProductId,
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Quantity = request.Quantity,
            FromWarehouseId = request.SourceWarehouseId,
            ToWarehouseId = request.TargetWarehouseId,
            Notes = request.Notes,
            Reason = $"Transfer: {sourceWarehouse.Name} → {targetWarehouse.Name}",
            Date = DateTime.UtcNow
        };
        inMovement.SetStockLevels(previousStock - request.Quantity, previousStock);
        inMovement.SetMovementType(StockMovementType.Transfer);

        await _movementRepository.AddAsync(outMovement).ConfigureAwait(false);
        await _movementRepository.AddAsync(inMovement).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TransferStockResult
        {
            IsSuccess = true,
            SourceRemainingStock = previousStock - request.Quantity,
            TargetNewStock = request.Quantity,
            MovementId = outMovement.Id
        };
    }
}
