using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Odeme islemi kaydi — PayTR uzerinden gerceklestirilen odemelerin takibi.
/// Her odeme denemesi icin ayri bir kayit tutulur.
/// </summary>
public sealed class PaymentTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public PaymentProviderType Provider { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    /// <summary>Odeme saglayicisinin islem referans numarasi.</summary>
    public string? TransactionId { get; set; }

    /// <summary>Taksit sayisi. Tek cekim icin 1.</summary>
    public int InstallmentCount { get; set; } = 1;

    /// <summary>Odemenin gerceklestigi zaman (UTC).</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Iade isleminin gerceklestigi zaman (UTC).</summary>
    public DateTime? RefundedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    // EF Core parametresiz ctor
    private PaymentTransaction() { }

    /// <summary>
    /// Factory method — yeni odeme islemi olusturur.
    /// </summary>
    public static PaymentTransaction Create(
        Guid tenantId,
        Guid orderId,
        PaymentProviderType provider,
        decimal amount,
        string currency = "TRY",
        int installmentCount = 1)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));

        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId bos olamaz.", nameof(orderId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Odeme tutari sifirdan buyuk olmalidir.");

        if (installmentCount < 1)
            throw new ArgumentOutOfRangeException(nameof(installmentCount), "Taksit sayisi en az 1 olmalidir.");

        return new PaymentTransaction
        {
            TenantId = tenantId,
            OrderId = orderId,
            Provider = provider,
            Amount = amount,
            Currency = currency,
            InstallmentCount = installmentCount,
            Status = PaymentTransactionStatus.Pending
        };
    }

    /// <summary>Odemeyi basarili olarak tamamlar.</summary>
    public void MarkCompleted(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("TransactionId bos olamaz.", nameof(transactionId));

        if (Status != PaymentTransactionStatus.Pending && Status != PaymentTransactionStatus.Processing)
            throw new InvalidOperationException(
                $"{Status} durumundaki islem tamamlanamaz. Sadece Pending veya Processing islemler tamamlanabilir.");

        TransactionId = transactionId;
        Status = PaymentTransactionStatus.Completed;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Odemeyi basarisiz olarak isaretler.</summary>
    public void MarkFailed()
    {
        if (Status == PaymentTransactionStatus.Completed || Status == PaymentTransactionStatus.Refunded)
            throw new InvalidOperationException(
                $"{Status} durumundaki islem basarisiz olarak isaretlenemez.");

        Status = PaymentTransactionStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Odemeyi iade edilmis olarak isaretler.</summary>
    public void MarkRefunded()
    {
        if (Status != PaymentTransactionStatus.Completed)
            throw new InvalidOperationException(
                $"Sadece Completed durumundaki islemler iade edilebilir. Mevcut durum: {Status}.");

        Status = PaymentTransactionStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"PaymentTx [{Provider}] {Amount:N2} {Currency} ({Status}) Order:{OrderId}";
}
