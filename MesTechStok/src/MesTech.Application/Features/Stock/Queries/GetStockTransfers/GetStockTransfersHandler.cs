using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockTransfers;

public sealed class GetStockTransfersHandler : IRequestHandler<GetStockTransfersQuery, IReadOnlyList<StockTransferItemDto>>
{
    private readonly IStockMovementRepository _movementRepo;

    public GetStockTransfersHandler(IStockMovementRepository movementRepo) => _movementRepo = movementRepo;

    public async Task<IReadOnlyList<StockTransferItemDto>> Handle(GetStockTransfersQuery request, CancellationToken ct)
    {
        var movements = await _movementRepo.GetRecentAsync(request.TenantId, request.Count, ct);

        return movements.Select(m => new StockTransferItemDto
        {
            Id = m.Id,
            ProductName = m.Notes ?? m.ProductId.ToString(),
            SKU = m.ProductId.ToString().Length > 8 ? m.ProductId.ToString().Substring(0, 8) : m.ProductId.ToString(),
            Quantity = m.Quantity,
            MovementType = m.MovementType,
            Reference = m.Reason ?? string.Empty,
            MovementDate = m.Date
        }).ToList().AsReadOnly();
    }
}
