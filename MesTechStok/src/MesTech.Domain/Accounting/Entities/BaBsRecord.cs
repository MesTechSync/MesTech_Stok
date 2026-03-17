using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Ba/Bs Beyanname kaydi — VUK 396 Sira No'lu Genel Teblig.
/// Ba: 5.000 TL ve uzeri alislar (tedarikci bazli, KDV dahil).
/// Bs: 5.000 TL ve uzeri satislar (musteri bazli, KDV dahil).
/// Son bildirim tarihi: takip eden ayin son gunu.
/// </summary>
public class BaBsRecord : BaseEntity, ITenantEntity
{
    /// <summary>
    /// Ba/Bs icin minimum bildirim esigi (TL, KDV dahil).
    /// VUK 396 Sira No: 5.000 TL ve uzeri islemler bildirilir.
    /// </summary>
    public const decimal MinimumThreshold = 5_000m;

    public Guid TenantId { get; set; }

    /// <summary>Beyanname yili.</summary>
    public int Year { get; private set; }

    /// <summary>Beyanname ayi (1-12).</summary>
    public int Month { get; private set; }

    /// <summary>Ba (alim) veya Bs (satim) formu.</summary>
    public BaBsType Type { get; private set; }

    /// <summary>Karsi tarafin VKN veya TCKN'si.</summary>
    public string CounterpartyVkn { get; private set; } = string.Empty;

    /// <summary>Karsi tarafin unvani / adi.</summary>
    public string CounterpartyName { get; private set; } = string.Empty;

    /// <summary>Donem toplam tutari (KDV dahil).</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Belge adedi.</summary>
    public int DocumentCount { get; private set; }

    /// <summary>Son bildirim tarihi (takip eden ayin son gunu).</summary>
    public DateTime Deadline { get; private set; }

    private BaBsRecord() { }

    public static BaBsRecord Create(
        Guid tenantId,
        int year,
        int month,
        BaBsType type,
        string counterpartyVkn,
        string counterpartyName,
        decimal totalAmount,
        int documentCount)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Ay 1-12 araliginda olmalidir.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Yil 2000-2100 araliginda olmalidir.");
        ArgumentException.ThrowIfNullOrWhiteSpace(counterpartyVkn);
        ArgumentException.ThrowIfNullOrWhiteSpace(counterpartyName);

        if (totalAmount < MinimumThreshold)
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount),
                $"Ba/Bs esigi alti tutar kaydedilemez. Minimum: {MinimumThreshold:N0} TL.");

        // Deadline: takip eden ayin son gunu
        var nextMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var deadline = new DateTime(nextMonth.Year, nextMonth.Month,
            DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month), 23, 59, 59, DateTimeKind.Utc);

        var record = new BaBsRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            Type = type,
            CounterpartyVkn = counterpartyVkn,
            CounterpartyName = counterpartyName,
            TotalAmount = Math.Round(totalAmount, 2),
            DocumentCount = documentCount,
            Deadline = deadline,
            CreatedAt = DateTime.UtcNow
        };

        record.RaiseDomainEvent(new BaBsRecordCreatedEvent
        {
            TenantId = tenantId,
            BaBsRecordId = record.Id,
            Type = type,
            Year = year,
            Month = month,
            CounterpartyVkn = counterpartyVkn,
            TotalAmount = totalAmount
        });

        return record;
    }
}
