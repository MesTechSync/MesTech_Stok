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
    public Guid AccountId { get; set; }

    public TransactionType Type { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public string? Description { get; set; }
    public string? DocumentNumber { get; set; }

    // İlişkili kayıtlar
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ReturnRequestId { get; set; }
    public PlatformType? Platform { get; set; }

    // Navigation
    public CustomerAccount? CustomerAccount { get; set; }
    public SupplierAccount? SupplierAccount { get; set; }

    public decimal NetAmount => DebitAmount - CreditAmount;

    public override string ToString() =>
        $"{Type} D:{DebitAmount:N2} C:{CreditAmount:N2} [{DocumentNumber}]";
}
