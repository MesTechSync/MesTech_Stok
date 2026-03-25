using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.AddStockLot;

public sealed class AddStockLotHandler : IRequestHandler<AddStockLotCommand, AddStockLotResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddStockLotHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

#pragma warning disable MA0051 // Method is too long — stock lot ingestion is a single cohesive operation
    public async Task<AddStockLotResult> Handle(AddStockLotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity <= 0)
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = "Lot miktarı pozitif olmalı." };

        if (string.IsNullOrWhiteSpace(request.LotNumber))
            return new AddStockLotResult { IsSuccess = false, ErrorMessage = "Lot numarası boş olamaz." };

        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
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

        await _movementRepository.AddAsync(movement).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
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
