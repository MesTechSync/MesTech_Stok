using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Constants;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.AddStockLot;

public sealed class AddStockLotHandler : IRequestHandler<AddStockLotCommand, AddStockLotResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<AddStockLotHandler> _logger;

    public AddStockLotHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ILogger<AddStockLotHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

#pragma warning disable MA0051 // Method is too long — stock lot ingestion is a single cohesive operation
    public async Task<AddStockLotResult> Handle(AddStockLotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity <= 0)
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = "Lot miktarı pozitif olmalı." };

        if (string.IsNullOrWhiteSpace(request.LotNumber))
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = "Lot numarası boş olamaz." };

        await using var lockHandle = await _lockService.AcquireLockAsync(
            $"stock:product:{request.ProductId}",
            expiry: DomainConstants.StockLockExpiry,
            waitTimeout: DomainConstants.StockLockWaitTimeout,
            cancellationToken).ConfigureAwait(false);

        if (lockHandle is null)
        {
            _logger.LogWarning("Stock lock alınamadı — ProductId={ProductId}", request.ProductId);
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = "Stok kilidi alınamadı. Lütfen tekrar deneyin." };
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product == null)
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        var previousStock = product.Stock;

        // InventoryLot oluştur
        var lot = new InventoryLot
        {
            TenantId = product.TenantId,
            ProductId = request.ProductId,
            LotNumber = request.LotNumber,
            ExpiryDate = request.ExpiryDate,
            ReceivedQty = request.Quantity,
            RemainingQty = request.Quantity,
            Status = LotStatus.Open,
            CreatedDate = DateTime.UtcNow
        };

        // Domain logic — stok artır + event fırlat
        product.AddStock(request.Quantity, $"Lot: {request.LotNumber}");

        // Hareket kaydı
        var movement = new StockMovement
        {
            TenantId = product.TenantId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            TotalCost = request.Quantity * request.UnitCost,
            SupplierId = request.SupplierId,
            ToWarehouseId = request.WarehouseId,
            BatchNumber = request.LotNumber,
            ExpiryDate = request.ExpiryDate,
            Reason = $"Lot girişi: {request.LotNumber}",
            Date = DateTime.UtcNow
        };
        movement.SetStockLevels(previousStock, product.Stock);
        movement.SetMovementType(StockMovementType.Purchase);

        await _movementRepository.AddAsync(movement, cancellationToken).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AddStockLotResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            LotId = lot.Id,
            MovementId = movement.Id
        };
    }
}
