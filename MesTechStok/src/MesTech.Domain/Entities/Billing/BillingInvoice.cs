using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Billing;

/// <summary>
/// MesTech'in musteriye kestigi platform faturasi (SaaS abonelik faturasi).
/// Bu, marketplace satici faturasi degil — MesTech → Tenant yonunde.
/// </summary>
public sealed class BillingInvoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal TaxRate { get; private set; } = 0.20m; // %20 KDV
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";
    public BillingInvoiceStatus Status { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public TenantSubscription? Subscription { get; private set; }

    private BillingInvoice() { }

    public static BillingInvoice Create(
        Guid tenantId, Guid subscriptionId, string invoiceNumber,
        decimal amount, string currencyCode = "TRY",
        decimal taxRate = 0.20m, int dueDays = 7)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        if (amount <= 0) throw new ArgumentException("Fatura tutari pozitif olmali.", nameof(amount));

        var taxAmount = Math.Round(amount * taxRate, 2);
        var now = DateTime.UtcNow;

        return new BillingInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            InvoiceNumber = invoiceNumber,
            Amount = amount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = amount + taxAmount,
            CurrencyCode = currencyCode,
            Status = BillingInvoiceStatus.Draft,
            IssueDate = now,
            DueDate = now.AddDays(dueDays),
            CreatedAt = now
        };
    }

    public void Send()
    {
        if (Status != BillingInvoiceStatus.Draft)
            throw new InvalidOperationException("Sadece taslak fatura gonderilebilir.");
        Status = BillingInvoiceStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid(string? transactionId = null)
    {
        Status = BillingInvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
        PaymentTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkOverdue()
    {
        if (Status == BillingInvoiceStatus.Paid || Status == BillingInvoiceStatus.Cancelled)
            return;
        Status = BillingInvoiceStatus.Overdue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = BillingInvoiceStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Fatura numarasi uretimi: MEST-2026-000001</summary>
    public static string GenerateInvoiceNumber(int sequence)
        => $"MEST-{DateTime.UtcNow.Year}-{sequence:D6}";
}
