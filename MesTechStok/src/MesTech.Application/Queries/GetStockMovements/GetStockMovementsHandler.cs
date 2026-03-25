using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetStockMovements;

public sealed class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    private readonly IStockMovementRepository _movementRepository;

    public GetStockMovementsHandler(IStockMovementRepository movementRepository)
    {
        _movementRepository = movementRepository;
    }

    public async Task<IReadOnlyList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProductId.HasValue)
        {
            var movements = await _movementRepository.GetByProductIdAsync(request.ProductId.Value);
            return movements.Adapt<List<StockMovementDto>>().AsReadOnly();
        }

        if (request.From.HasValue && request.To.HasValue)
        {
            var movements = await _movementRepository.GetByDateRangeAsync(request.From.Value, request.To.Value);
            return movements.Adapt<List<StockMovementDto>>().AsReadOnly();
        }

        return new List<StockMovementDto>().AsReadOnly();
    }
}
