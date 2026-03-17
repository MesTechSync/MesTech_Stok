using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;

/// <summary>
/// Stopaj (tevkifat) kayitlarini listeleme sorgusu.
/// Tarih araligi opsiyonel — null ise tum kayitlar doner.
/// </summary>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="StartDate">Baslangic tarihi (dahil, opsiyonel).</param>
/// <param name="EndDate">Bitis tarihi (dahil, opsiyonel).</param>
public record ListTaxWithholdingsQuery(
    Guid TenantId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IReadOnlyList<TaxWithholdingDto>>;
