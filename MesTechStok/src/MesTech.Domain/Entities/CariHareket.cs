using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Cari hesap hareketi (borç/alacak) — OnMuhasebe modülü için.
/// </summary>
public class CariHareket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CariHesapId { get; set; }
    public decimal Amount { get; set; }
    public CariDirection Direction { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }

    // Navigation
    public CariHesap CariHesap { get; set; } = null!;
}
