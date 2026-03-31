using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.ListFixedAssets;

/// <summary>
/// Sabit kiymet listeleme handler.
/// IFixedAssetRepository uzerinden tenant ve aktiflik filtresiyle sorgular.
/// </summary>
public sealed class ListFixedAssetsHandler : IRequestHandler<ListFixedAssetsQuery, IReadOnlyList<FixedAssetDto>>
{
    private readonly IFixedAssetRepository _repository;

    public ListFixedAssetsHandler(IFixedAssetRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<FixedAssetDto>> Handle(ListFixedAssetsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assets = await _repository.GetAllAsync(request.TenantId, request.IsActive, cancellationToken).ConfigureAwait(false);
        return assets.Select(a => new FixedAssetDto
        {
            Id = a.Id,
            Name = a.Name,
            AssetCode = a.AssetCode,
            AcquisitionCost = a.AcquisitionCost,
            AcquisitionDate = a.AcquisitionDate,
            UsefulLifeYears = a.UsefulLifeYears,
            Method = a.Method.ToString(),
            AccumulatedDepreciation = a.AccumulatedDepreciation,
            NetBookValue = a.NetBookValue,
            IsActive = a.IsActive,
            Description = a.Description
        }).ToList().AsReadOnly();
    }
}
