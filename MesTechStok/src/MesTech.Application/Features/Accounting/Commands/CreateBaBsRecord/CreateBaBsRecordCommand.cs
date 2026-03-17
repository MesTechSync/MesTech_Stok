using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;

/// <summary>
/// Ba/Bs kayit olusturma komutu — VUK 396 Sira No'lu Genel Teblig.
/// Ba: 5.000 TL ve uzeri alislar (tedarikci bazli, KDV dahil).
/// Bs: 5.000 TL ve uzeri satislar (musteri bazli, KDV dahil).
/// </summary>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="Year">Beyanname yili.</param>
/// <param name="Month">Beyanname ayi (1-12).</param>
/// <param name="Type">Ba (alim) veya Bs (satim) formu.</param>
/// <param name="CounterpartyVkn">Karsi tarafin VKN veya TCKN'si.</param>
/// <param name="CounterpartyName">Karsi tarafin unvani / adi.</param>
/// <param name="TotalAmount">Donem toplam tutari (KDV dahil).</param>
/// <param name="DocumentCount">Belge adedi.</param>
public record CreateBaBsRecordCommand(
    Guid TenantId,
    int Year,
    int Month,
    BaBsType Type,
    string CounterpartyVkn,
    string CounterpartyName,
    decimal TotalAmount,
    int DocumentCount
) : IRequest<Guid>;
