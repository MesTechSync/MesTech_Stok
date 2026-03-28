using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockTransfers;

public sealed class GetStockTransfersHandler : IRequestHandler<GetStockTransfersQuery, IReadOnlyList<StockTransferItemDto>>
{
    private readonly IStockMovementRepository _movementRepo;

    public GetStockTransfersHandler(IStockMovementRepository movementRepo) => _movementRepo = movementRepo;

    public async Task<IReadOnlyList<StockTransferItemDto>> Handle(GetStockTransfersQuery request, CancellationToken cancellationToken)
    {
        var movements = await _movementRepo.GetRecentAsync(request.TenantId, request.Count, cancellationToken);

        return movements.Select(m => new StockTransferItemDto
        {
            Id = m.Id,
            ProductName = m.ProductName ?? string.Empty,
            SKU = m.ProductSKU ?? string.Empty,
            Quantity = m.Quantity,
            MovementType = m.MovementType,
            Reference = m.Reason ?? string.Empty,
            MovementDate = m.Date
        }).ToList().AsReadOnly();
    }
}
