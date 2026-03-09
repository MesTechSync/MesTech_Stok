using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform ödeme kaydı — platformdan satıcıya yapılan ödemeler.
/// Her platform farklı ödeme takvimi uygular (Trendyol haftalık, HB 2 haftalık vb.).
/// Settlement (hesap kesimi) dönemi bazında takip edilir.
/// </summary>
public class PlatformPayment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }

    public PlatformType Platform { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Dönem bilgisi
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? ScheduledPaymentDate { get; set; }
    public DateTime? ActualPaymentDate { get; set; }

    // Tutarlar
    public decimal GrossSales { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalShippingCost { get; set; }
    public decimal TotalReturnDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    // Sipariş sayıları
    public int OrderCount { get; set; }
    public int ReturnCount { get; set; }

    // Banka bilgisi
    public string? BankReference { get; set; }
    public string? PlatformPaymentId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Store? Store { get; set; }

    public void CalculateNetAmount()
    {
        NetAmount = GrossSales - TotalCommission - TotalShippingCost - TotalReturnDeduction - OtherDeductions;
    }

    public void MarkAsCompleted(string? bankReference = null)
    {
        Status = PaymentStatus.Completed;
        ActualPaymentDate = DateTime.UtcNow;
        BankReference = bankReference;
    }

    public void MarkAsFailed(string? reason = null)
    {
        Status = PaymentStatus.Failed;
        Notes = reason;
    }

    public bool IsOverdue => Status == PaymentStatus.Pending
        && ScheduledPaymentDate.HasValue
        && ScheduledPaymentDate.Value < DateTime.UtcNow;

    public override string ToString() =>
        $"{Platform} [{PeriodStart:dd.MM}-{PeriodEnd:dd.MM}] Net:{NetAmount:N2} ({Status})";
}
