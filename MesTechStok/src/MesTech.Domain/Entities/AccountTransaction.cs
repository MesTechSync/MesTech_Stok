using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Cari hesap hareketi — immutable. Bir kez yazılır, düzeltilemez.
/// Düzeltme gerekirse ters hareket (Adjustment) yazılır.
/// Debit = borç, Credit = alacak.
/// </summary>
public class AccountTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AccountId { get; internal set; }

    public TransactionType Type { get; internal set; }
    public decimal DebitAmount { get; internal set; }
    public decimal CreditAmount { get; internal set; }
    public string Currency { get; internal set; } = "TRY";

    public DateTime TransactionDate { get; internal set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; internal set; }

    public string? Description { get; internal set; }
    public string? DocumentNumber { get; internal set; }

    // İlişkili kayıtlar
    public Guid? InvoiceId { get; internal set; }
    public Guid? OrderId { get; internal set; }
    public Guid? ReturnRequestId { get; internal set; }
    public PlatformType? Platform { get; internal set; }

    // Navigation
    public CustomerAccount? CustomerAccount { get; set; }
    public SupplierAccount? SupplierAccount { get; set; }

    internal AccountTransaction() { } // EF Core + Domain internal use

    /// <summary>
    /// Immutable factory — bir kez yazılır, düzeltilemez.
    /// Düzeltme gerekirse ters hareket (Adjustment) yazılır.
    /// </summary>
    public static AccountTransaction Create(
        Guid tenantId, Guid accountId, TransactionType type,
        decimal debitAmount, decimal creditAmount,
        string? description = null, string? documentNumber = null,
        string currency = "TRY", DateTime? transactionDate = null,
        DateTime? dueDate = null, Guid? invoiceId = null,
        Guid? orderId = null, Guid? returnRequestId = null,
        PlatformType? platform = null)
    {
        if (debitAmount < 0) throw new ArgumentException("Borç tutarı negatif olamaz.", nameof(debitAmount));
        if (creditAmount < 0) throw new ArgumentException("Alacak tutarı negatif olamaz.", nameof(creditAmount));
        if (debitAmount == 0 && creditAmount == 0)
            throw new ArgumentException("Borç ve alacak aynı anda sıfır olamaz.", nameof(debitAmount));

        return new AccountTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            Type = type,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            Currency = currency,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
            DueDate = dueDate,
            Description = description,
            DocumentNumber = documentNumber,
            InvoiceId = invoiceId,
            OrderId = orderId,
            ReturnRequestId = returnRequestId,
            Platform = platform,
            CreatedAt = DateTime.UtcNow
        };
    }

    public decimal NetAmount => DebitAmount - CreditAmount;

    public override string ToString() =>
        $"{Type} D:{DebitAmount:N2} C:{CreditAmount:N2} [{DocumentNumber}]";
}
