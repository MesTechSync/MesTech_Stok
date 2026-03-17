using MediatR;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;

/// <summary>
/// Ba/Bs beyanname raporu sorgusu — VUK 396.
/// Belirtilen ay/yil icin 5.000 TL ustu alis (Ba) ve satis (Bs) formlarini olusturur.
/// </summary>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="Year">Beyanname yili.</param>
/// <param name="Month">Beyanname ayi (1-12).</param>
public record GenerateBaBsReportQuery(Guid TenantId, int Year, int Month)
    : IRequest<BaBsReportDto>;
