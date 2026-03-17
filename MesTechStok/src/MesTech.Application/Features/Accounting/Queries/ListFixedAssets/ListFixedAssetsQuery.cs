using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.ListFixedAssets;

/// <summary>
/// Sabit kiymet listeleme sorgusu — VUK md. 313.
/// IsActive null ise tum kayitlar, true/false ise filtrelenmis sonuc doner.
/// </summary>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="IsActive">Aktiflik filtresi (null = tumu).</param>
public record ListFixedAssetsQuery(Guid TenantId, bool? IsActive = null)
    : IRequest<IReadOnlyList<FixedAssetDto>>;
