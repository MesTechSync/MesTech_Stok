using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;

/// <summary>
/// Sabit kiymet pasife alma handler.
/// Entity'yi yukler, Deactivate() domain metodunu calistirir, kaydeder.
/// </summary>
public class DeactivateFixedAssetHandler : IRequestHandler<DeactivateFixedAssetCommand, Unit>
{
    private readonly IFixedAssetRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeactivateFixedAssetHandler(IFixedAssetRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Unit> Handle(DeactivateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"FixedAsset bulunamadi: {request.Id}");

        asset.Deactivate();

        await _repository.UpdateAsync(asset, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
