using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Fatura entegrator kontor bakiyesi — provider bazinda takip.
/// Composite unique: (StoreId, Provider) — bir store'un bir provider'i icin tek kayit.
/// </summary>
public class KontorBalance : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public InvoiceProvider Provider { get; set; }
    public int RemainingKontor { get; set; }
    public int TotalKontor { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public Store Store { get; set; } = null!;

    public void UpdateBalance(int remaining, int total)
    {
        RemainingKontor = remaining;
        TotalKontor = total;
        LastCheckedAt = DateTime.UtcNow;
    }

    public bool IsLow(int threshold = 50) => RemainingKontor <= threshold;
}
