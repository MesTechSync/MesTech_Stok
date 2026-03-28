using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedAssets;

public sealed class GetFixedAssetsHandler : IRequestHandler<GetFixedAssetsQuery, IReadOnlyList<FixedAssetDto>>
{
    private readonly IFixedAssetRepository _repository;

    public GetFixedAssetsHandler(IFixedAssetRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<FixedAssetDto>> Handle(GetFixedAssetsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assets = await _repository.GetAllAsync(request.TenantId, request.IsActive, cancellationToken);
        return assets.Adapt<List<FixedAssetDto>>().AsReadOnly();
    }
}
