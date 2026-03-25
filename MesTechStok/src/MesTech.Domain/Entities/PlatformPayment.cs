using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform ödeme kaydı — platformdan satıcıya yapılan ödemeler.
/// Her platform farklı ödeme takvimi uygular (Trendyol haftalık, HB 2 haftalık vb.).
/// Settlement (hesap kesimi) dönemi bazında takip edilir.
/// </summary>
public sealed class PlatformPayment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }

    public PlatformType Platform { get; set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    // Dönem bilgisi
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? ScheduledPaymentDate { get; set; }
    public DateTime? ActualPaymentDate { get; private set; }

    // Tutarlar
    public decimal GrossSales { get; private set; }
    public decimal TotalCommission { get; private set; }
    public decimal TotalShippingCost { get; private set; }
    public decimal TotalReturnDeduction { get; private set; }
    public decimal OtherDeductions { get; private set; }
    public decimal NetAmount { get; private set; }
    public string Currency { get; set; } = "TRY";

    // Sipariş sayıları
    public int OrderCount { get; set; }
    public int ReturnCount { get; set; }

    // Banka bilgisi
    public string? BankReference { get; private set; }
    public string? PlatformPaymentId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Store? Store { get; set; }

    public void SetAmounts(decimal grossSales, decimal commission, decimal shippingCost,
        decimal returnDeduction, decimal otherDeductions)
    {
        GrossSales = grossSales;
        TotalCommission = commission;
        TotalShippingCost = shippingCost;
        TotalReturnDeduction = returnDeduction;
        OtherDeductions = otherDeductions;
        CalculateNetAmount();
    }

    public void CalculateNetAmount()
    {
        if (GrossSales < 0)
            throw new InvalidOperationException("GrossSales must be non-negative.");

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
        RaiseDomainEvent(new PaymentFailedEvent(TenantId, Id, reason, "PAYMENT_FAILED", 1, DateTime.UtcNow));
    }

    public bool IsOverdue => Status == PaymentStatus.Pending
        && ScheduledPaymentDate.HasValue
        && ScheduledPaymentDate.Value < DateTime.UtcNow;

    public override string ToString() =>
        $"{Platform} [{PeriodStart:dd.MM}-{PeriodEnd:dd.MM}] Net:{NetAmount:N2} ({Status})";
}
