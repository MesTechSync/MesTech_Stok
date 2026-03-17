using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;

/// <summary>
/// Sabit kiymet guncelleme handler.
/// Entity'yi yukler, duzenlenebilir alanlari gunceller, kaydeder.
/// AcquisitionCost immutable oldugu icin degistirilmez.
/// </summary>
public class UpdateFixedAssetHandler : IRequestHandler<UpdateFixedAssetCommand, Unit>
{
    private readonly IFixedAssetRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateFixedAssetHandler(IFixedAssetRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Unit> Handle(UpdateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"FixedAsset bulunamadi: {request.Id}");

        asset.Update(request.Name, request.Description, request.UsefulLifeYears);

        await _repository.UpdateAsync(asset, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
