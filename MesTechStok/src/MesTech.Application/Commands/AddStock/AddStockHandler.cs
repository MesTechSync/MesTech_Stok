using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Constants;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.AddStock;

public sealed class AddStockHandler : IRequestHandler<AddStockCommand, AddStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AddStockHandler> _logger;

    public AddStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ITenantProvider tenantProvider,
        ILogger<AddStockHandler> logger)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AddStockResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var lockHandle = await _lockService.AcquireLockAsync(
            $"stock:product:{request.ProductId}",
            expiry: DomainConstants.StockLockExpiry,
            waitTimeout: DomainConstants.StockLockWaitTimeout,
            cancellationToken).ConfigureAwait(false);

        if (lockHandle is null)
        {
            _logger.LogWarning("Stock lock alınamadı — ProductId={ProductId}", request.ProductId);
            return new AddStockResult { IsSuccess = false, ErrorMessage = "Stok kilidi alınamadı. Lütfen tekrar deneyin." };
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
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

        await _movementRepository.AddAsync(movement, cancellationToken).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AddStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
