using Mapster;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetStockLotById;

public sealed class GetStockLotByIdHandler : IRequestHandler<GetStockLotByIdQuery, GetStockLotByIdResult?>
{
    private readonly IStockLotRepository _repository;

    public GetStockLotByIdHandler(IStockLotRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<GetStockLotByIdResult?> Handle(GetStockLotByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<GetStockLotByIdResult>();
    }
}
