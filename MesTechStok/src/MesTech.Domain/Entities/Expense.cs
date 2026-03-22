using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Gider kaydı — OnMuhasebe modülü için.
/// </summary>
public class Expense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; set; } = "TRY";
    public ExpenseType ExpenseType { get; set; }
    public ExpenseCategory Category { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? InvoiceNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePeriod { get; set; }

    // ── Domain Logic ──

    public void SetAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Gider tutarı pozitif olmalı.", nameof(amount));
        Amount = amount;
    }

    public void MarkAsProcessing()
    {
        if (PaymentStatus != PaymentStatus.Pending)
            throw new InvalidOperationException($"Sadece bekleyen gider işleme alınabilir. Mevcut: {PaymentStatus}");
        PaymentStatus = PaymentStatus.Processing;
    }

    public void MarkAsCompleted()
    {
        if (PaymentStatus is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidOperationException($"Sadece bekleyen veya işlenen gider tamamlanabilir. Mevcut: {PaymentStatus}");
        PaymentStatus = PaymentStatus.Completed;
    }

    public void Cancel()
    {
        if (PaymentStatus == PaymentStatus.Completed)
            throw new InvalidOperationException("Tamamlanmış gider iptal edilemez.");
        PaymentStatus = PaymentStatus.Cancelled;
    }
}
