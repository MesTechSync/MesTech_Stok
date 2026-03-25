using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi cari hesabı (dropshipping tedarikçisi dahil).
/// Balance = sum(debit) - sum(credit).
/// Negatif bakiye = biz tedarikçiye borçluyuz.
/// Transaction log immutable.
/// </summary>
public sealed class SupplierAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; set; }

    public string AccountCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierTaxNumber { get; set; }
    public string? SupplierTaxOffice { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierEmail { get; set; }
    public string? SupplierPhone { get; set; }

    public int PaymentTermDays { get; private set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; private set; } = true;

    public void SetPaymentTerms(int days)
    {
        if (days < 0) throw new ArgumentException("Ödeme vadesi negatif olamaz.", nameof(days));
        PaymentTermDays = days;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    // Navigation
    public Supplier? Supplier { get; set; }

    private readonly List<AccountTransaction> _transactions = new();
    public IReadOnlyCollection<AccountTransaction> Transactions => _transactions.AsReadOnly();

    public decimal Balance => _transactions.Sum(t => t.DebitAmount - t.CreditAmount);

    public void AddTransaction(AccountTransaction transaction)
    {
        transaction.AccountId = Id;
        _transactions.Add(transaction);
    }

    public AccountTransaction RecordPurchase(Guid invoiceId, decimal amount, string invoiceNumber)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.PurchaseInvoice,
            DebitAmount = 0,
            CreditAmount = amount,
            InvoiceId = invoiceId,
            DocumentNumber = invoiceNumber,
            Description = $"Alış faturası: {invoiceNumber}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public AccountTransaction RecordPayment(decimal amount, string? documentNumber = null, DateTime? dueDate = null)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.Payment,
            DebitAmount = amount,
            CreditAmount = 0,
            DocumentNumber = documentNumber,
            DueDate = dueDate,
            Description = $"Ödeme: {amount:N2} {Currency}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public AccountTransaction RecordPurchaseReturn(Guid returnRequestId, decimal amount)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.PurchaseReturn,
            DebitAmount = amount,
            CreditAmount = 0,
            ReturnRequestId = returnRequestId,
            Description = $"Alış iadesi: {amount:N2} {Currency}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public decimal OverdueBalance(DateTime asOf)
    {
        return _transactions
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < asOf)
            .Sum(t => t.DebitAmount - t.CreditAmount);
    }

    public override string ToString() => $"Tedarikçi [{AccountCode}] {SupplierName} Bakiye:{Balance:N2}";
}
