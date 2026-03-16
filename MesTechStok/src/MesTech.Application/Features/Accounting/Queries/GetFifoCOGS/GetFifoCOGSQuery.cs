using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;

/// <summary>
/// FIFO yontemi ile satilan mal maliyeti (COGS) sorgular.
/// ProductId belirtilirse tek urun, belirtilmezse tum tenant urunleri icin hesaplar.
/// </summary>
public record GetFifoCOGSQuery(Guid TenantId, Guid? ProductId = null)
    : IRequest<IReadOnlyList<FifoCostResultDto>>;
