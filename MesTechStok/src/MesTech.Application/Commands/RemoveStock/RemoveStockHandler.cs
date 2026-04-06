using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Constants;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.RemoveStock;

public sealed class RemoveStockHandler : IRequestHandler<RemoveStockCommand, RemoveStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly StockCalculationService _stockCalc;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RemoveStockHandler> _logger;

    public RemoveStockHandler(
        IProductRepository productRepository,
        IStockMovementRepository movementRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        StockCalculationService stockCalc,
        ITenantProvider tenantProvider,
        ILogger<RemoveStockHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _stockCalc = stockCalc ?? throw new ArgumentNullException(nameof(stockCalc));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RemoveStockResult> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
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
            return new RemoveStockResult { IsSuccess = false, ErrorMessage = "Stok kilidi alınamadı. Lütfen tekrar deneyin." };
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
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
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Quantity = -request.Quantity,
            Reason = request.Reason,
            DocumentNumber = request.DocumentNumber,
            Date = DateTime.UtcNow
        };
        movement.SetStockLevels(previousStock, product.Stock);
        movement.SetMovementType(StockMovementType.StockOut);

        await _movementRepository.AddAsync(movement, cancellationToken).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RemoveStockResult
        {
            IsSuccess = true,
            NewStockLevel = product.Stock,
            MovementId = movement.Id
        };
    }
}
