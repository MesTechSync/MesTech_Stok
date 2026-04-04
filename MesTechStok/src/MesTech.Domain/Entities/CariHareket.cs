using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Cari hesap hareketi (borç/alacak) — OnMuhasebe modülü için.
/// </summary>
public sealed class CariHareket : BaseEntity, ITenantEntity
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

    public static CariHareket Create(
        Guid tenantId, Guid cariHesapId, decimal amount,
        CariDirection direction, string description,
        DateTime? date = null, Guid? invoiceId = null, Guid? orderId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        return new CariHareket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CariHesapId = cariHesapId,
            Amount = amount,
            Direction = direction,
            Description = description,
            Date = date ?? DateTime.UtcNow,
            InvoiceId = invoiceId,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
