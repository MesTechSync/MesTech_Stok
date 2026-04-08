using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Vergi takvimi kalemi — KDV, Muhtasar, Gecici Vergi, Ba-Bs vb. son odeme/gonderim tarihi.
/// Turkiye vergi takvimi preset'leri ile seed edilir.
/// </summary>
public sealed class TaxCalendarItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string TaxType { get; private set; } = string.Empty;
    public int DueDay { get; private set; }
    public int DueMonth { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsAutoCalculated { get; private set; }
    public TaxCalendarFrequency Frequency { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    private TaxCalendarItem() { }

    public static TaxCalendarItem Create(
        Guid tenantId, string taxType, int dueDay, int dueMonth,
        string description, TaxCalendarFrequency frequency,
        bool isAutoCalculated = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(taxType);
        if (dueDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(dueDay));

        return new TaxCalendarItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TaxType = taxType,
            DueDay = dueDay,
            DueMonth = dueMonth,
            Description = description,
            Frequency = frequency,
            IsAutoCalculated = isAutoCalculated,
            CreatedAt = DateTime.UtcNow
        };
    }

    public DateTime GetNextDueDate(DateTime from)
    {
        var year = from.Year;
        var month = DueMonth == 0 ? from.Month : DueMonth;
        var day = Math.Min(DueDay, DateTime.DaysInMonth(year, month));
        var due = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        return due <= from ? due.AddMonths(Frequency == TaxCalendarFrequency.Monthly ? 1 : 3) : due;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}

public enum TaxCalendarFrequency
{
    Monthly = 0,
    Quarterly = 1,
    Annual = 2
}
