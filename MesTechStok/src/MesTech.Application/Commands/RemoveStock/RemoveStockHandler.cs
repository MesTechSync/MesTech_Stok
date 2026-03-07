using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;

namespace MesTech.Application.Commands.RemoveStock;

public class RemoveStockHandler : IRequestHandler<RemoveStockCommand, RemoveStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StockCalculationService _stockCalc;

    public RemoveStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        StockCalculationService stockCalc)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockCalc = stockCalc ?? throw new ArgumentNullException(nameof(stockCalc));
    }

    public async Task<RemoveStockResult> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
            return new RemoveStockResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        try
        {
            _stockCalc.ValidateStockSufficiency(product, request.Quantity);
        }
        catch (InsufficientStockException ex)
        {
            return new RemoveStockResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        var previousStock = product.Stock;
        product.AdjustStock(-request.Quantity, StockMovementType.StockOut);

        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            Quantity = -request.Quantity,
            PreviousStock = previousStock,
            NewStock = product.Stock,
            NewStockLevel = product.Stock,
            Reason = request.Reason,
            DocumentNumber = request.DocumentNumber,
            Date = DateTime.UtcNow
        };
        movement.SetMovementType(StockMovementType.StockOut);

        await _movementRepository.AddAsync(movement).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RemoveStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
