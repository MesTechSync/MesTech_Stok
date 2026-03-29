using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.AdjustStock;

public sealed class AdjustStockHandler : IRequestHandler<AdjustStockCommand, AdjustStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<AdjustStockHandler> _logger;

    public AdjustStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ILogger<AdjustStockHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdjustStockResult> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var lockHandle = await _lockService.AcquireLockAsync(
            $"stock:product:{request.ProductId}",
            expiry: TimeSpan.FromSeconds(30),
            waitTimeout: TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);

        if (lockHandle is null)
        {
            _logger.LogWarning("Stock lock alınamadı — ProductId={ProductId}", request.ProductId);
            return new AdjustStockResult { IsSuccess = false, ErrorMessage = "Stok kilidi alınamadı. Lütfen tekrar deneyin." };
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
            return new AdjustStockResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        var previousStock = product.Stock;

        // Domain logic — event fırlatır
        product.AdjustStock(request.Quantity, StockMovementType.Adjustment, request.Reason);

        // Hareket kaydı
        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Reason = request.Reason,
            ProcessedBy = request.PerformedBy,
            Date = DateTime.UtcNow
        };
        movement.SetStockLevels(previousStock, product.Stock);
        movement.SetMovementType(StockMovementType.Adjustment);

        await _movementRepository.AddAsync(movement).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AdjustStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
