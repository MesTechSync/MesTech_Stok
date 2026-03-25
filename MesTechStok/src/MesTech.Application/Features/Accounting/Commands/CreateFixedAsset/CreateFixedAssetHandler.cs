using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;

/// <summary>
/// Yeni sabit kiymet olusturma handler.
/// FixedAsset entity'sini olusturur, domain event'i tetikler, veritabanina kaydeder.
/// </summary>
public sealed class CreateFixedAssetHandler : IRequestHandler<CreateFixedAssetCommand, Guid>
{
    private readonly IFixedAssetRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateFixedAssetHandler(IFixedAssetRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var asset = FixedAsset.Create(
            tenantId: request.TenantId,
            name: request.Name,
            assetCode: request.AssetCode,
            acquisitionCost: request.AcquisitionCost,
            acquisitionDate: request.AcquisitionDate,
            usefulLifeYears: request.UsefulLifeYears,
            method: request.Method,
            description: request.Description);

        await _repository.AddAsync(asset, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return asset.Id;
    }
}
