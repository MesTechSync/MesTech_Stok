using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetStockMovements;

public sealed class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    private readonly IStockMovementRepository _movementRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetStockMovementsHandler(
        IStockMovementRepository movementRepository,
        ITenantProvider tenantProvider)
    {
        _movementRepository = movementRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProductId.HasValue)
        {
            var movements = await _movementRepository.GetByProductIdAsync(request.ProductId.Value, cancellationToken).ConfigureAwait(false);
            return movements.Adapt<List<StockMovementDto>>().AsReadOnly();
        }

        if (request.From.HasValue && request.To.HasValue)
        {
            var movements = await _movementRepository.GetByDateRangeAsync(request.From.Value, request.To.Value, cancellationToken).ConfigureAwait(false);
            return movements.Adapt<List<StockMovementDto>>().AsReadOnly();
        }

        // Filtre yoksa son 50 hareketi göster — boş sayfa yerine kullanıcıya veri sun
        var recent = await _movementRepository.GetRecentAsync(
            _tenantProvider.GetCurrentTenantId(), 50, cancellationToken);
        return recent.Adapt<List<StockMovementDto>>().AsReadOnly();
    }
}
