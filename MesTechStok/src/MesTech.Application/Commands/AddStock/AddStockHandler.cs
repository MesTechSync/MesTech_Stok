using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.AddStock;

public class AddStockHandler : IRequestHandler<AddStockCommand, AddStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddStockResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return new AddStockResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        var previousStock = product.Stock;

        // Domain logic — event fırlatır
        product.AdjustStock(request.Quantity, StockMovementType.StockIn);

        // Hareket kaydı
        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            PreviousStock = previousStock,
            NewStock = product.Stock,
            NewStockLevel = product.Stock,
            UnitCost = request.UnitCost,
            TotalCost = request.Quantity * request.UnitCost,
            BatchNumber = request.BatchNumber,
            ExpiryDate = request.ExpiryDate,
            DocumentNumber = request.DocumentNumber,
            Reason = request.Reason,
            Date = DateTime.UtcNow
        };
        movement.SetMovementType(StockMovementType.StockIn);

        await _movementRepository.AddAsync(movement);
        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
