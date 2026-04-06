using Mapster;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetStockMovementById;

public sealed class GetStockMovementByIdHandler : IRequestHandler<GetStockMovementByIdQuery, StockMovementDto?>
{
    private readonly IStockMovementRepository _repository;

    public GetStockMovementByIdHandler(IStockMovementRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<StockMovementDto?> Handle(GetStockMovementByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<StockMovementDto>();
    }
}
