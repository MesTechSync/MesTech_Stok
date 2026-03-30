using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.ApproveReturn;

/// <summary>
/// Iade talebini onaylar, stok geri yukleme yapar ve ReturnResolvedEvent yayinlar.
/// </summary>
public sealed class ApproveReturnHandler : IRequestHandler<ApproveReturnCommand, ApproveReturnResult>
{
    private readonly IReturnRequestRepository _returnRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveReturnHandler(
        IReturnRequestRepository returnRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork)
    {
        _returnRepo = returnRepo ?? throw new ArgumentNullException(nameof(returnRepo));
        _productRepo = productRepo ?? throw new ArgumentNullException(nameof(productRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ApproveReturnResult> Handle(
        ApproveReturnCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var returnRequest = await _returnRepo.GetByIdAsync(request.ReturnRequestId);
        if (returnRequest is null)
            return new ApproveReturnResult
            {
                IsSuccess = false,
                ErrorMessage = $"ReturnRequest {request.ReturnRequestId} bulunamadi."
            };

        // Domain method — throws if status is not Pending
        returnRequest.Approve();

        bool stockRestored = false;

        // Auto-restore stock: iade edilen urunlerin stogunu artir
        if (request.AutoRestoreStock && !returnRequest.StockRestored)
        {
            var productIds = returnRequest.Lines
                .Where(l => l.ProductId.HasValue && l.Quantity > 0)
                .Select(l => l.ProductId!.Value)
                .Distinct()
                .ToList();

            // Batch fetch — eliminates N+1 query
            var products = (await _productRepo.GetByIdsAsync(productIds, cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var line in returnRequest.Lines)
            {
                if (line.ProductId.HasValue && line.Quantity > 0
                    && products.TryGetValue(line.ProductId.Value, out var product))
                {
                    product.AdjustStock(line.Quantity, StockMovementType.StockIn);
                    await _productRepo.UpdateAsync(product);
                }
            }

            returnRequest.MarkStockRestored();
            stockRestored = true;
        }

        await _returnRepo.UpdateAsync(returnRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveReturnResult
        {
            IsSuccess = true,
            StockRestored = stockRestored
        };
    }
}
