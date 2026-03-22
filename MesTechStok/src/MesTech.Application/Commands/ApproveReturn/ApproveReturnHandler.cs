using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.ApproveReturn;

/// <summary>
/// Iade talebini onaylar, stok geri yukleme yapar ve ReturnResolvedEvent yayinlar.
/// </summary>
public class ApproveReturnHandler : IRequestHandler<ApproveReturnCommand, ApproveReturnResult>
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
            foreach (var line in returnRequest.Lines)
            {
                if (line.ProductId.HasValue && line.Quantity > 0)
                {
                    var product = await _productRepo.GetByIdAsync(line.ProductId.Value);
                    if (product is not null)
                    {
                        product.AdjustStock(line.Quantity, StockMovementType.StockIn);
                        await _productRepo.UpdateAsync(product);
                    }
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
