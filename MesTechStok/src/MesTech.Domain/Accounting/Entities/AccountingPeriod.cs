using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Muhasebe donemi — ay bazli acma/kapama.
/// Kapali donemde kayit yapilamaz.
/// </summary>
public sealed class AccountingPeriod : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedByUserId { get; private set; }

    private AccountingPeriod() { }

    public static AccountingPeriod Create(Guid tenantId, int year, int month)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        return new AccountingPeriod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            StartDate = start,
            EndDate = end,
            IsClosed = false
        };
    }

    public void Close(string userId)
    {
        if (IsClosed)
            throw new InvalidOperationException($"Donem {Year}/{Month:D2} zaten kapali.");

        IsClosed = true;
        ClosedAt = DateTime.UtcNow;
        ClosedByUserId = userId;
    }

    public void Reopen()
    {
        if (!IsClosed)
            throw new InvalidOperationException($"Donem {Year}/{Month:D2} zaten acik.");

        IsClosed = false;
        ClosedAt = null;
        ClosedByUserId = null;
    }

    public bool ContainsDate(DateTime date)
        => date >= StartDate && date <= EndDate;

    public override string ToString()
        => $"{Year}/{Month:D2} ({(IsClosed ? "Kapali" : "Acik")})";
}
