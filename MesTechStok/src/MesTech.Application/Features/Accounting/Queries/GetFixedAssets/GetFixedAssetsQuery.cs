using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedAssets;

public record GetFixedAssetsQuery(
    Guid TenantId,
    bool? IsActive = null
) : IRequest<IReadOnlyList<FixedAssetDto>>;
