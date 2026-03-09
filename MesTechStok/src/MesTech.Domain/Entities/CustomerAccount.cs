using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Müşteri cari hesabı.
/// Balance = sum(debit) - sum(credit).
/// Pozitif bakiye = müşteri bize borçlu.
/// Transaction log immutable — düzeltme ters hareketle yapılır.
/// </summary>
public class CustomerAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }

    public string AccountCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;

    // Navigation
    public Customer? Customer { get; set; }

    private readonly List<AccountTransaction> _transactions = new();
    public IReadOnlyCollection<AccountTransaction> Transactions => _transactions.AsReadOnly();

    public decimal Balance => _transactions.Sum(t => t.DebitAmount - t.CreditAmount);

    public void AddTransaction(AccountTransaction transaction)
    {
        transaction.AccountId = Id;
        _transactions.Add(transaction);
    }

    public AccountTransaction RecordSale(Guid invoiceId, Guid orderId, decimal amount, string invoiceNumber, PlatformType? platform = null)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.SalesInvoice,
            DebitAmount = amount,
            CreditAmount = 0,
            DocumentNumber = invoiceNumber,
            InvoiceId = invoiceId,
            OrderId = orderId,
            Platform = platform,
            Description = $"Satış faturası: {invoiceNumber}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public AccountTransaction RecordCollection(decimal amount, string? documentNumber = null)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.Collection,
            DebitAmount = 0,
            CreditAmount = amount,
            DocumentNumber = documentNumber,
            Description = $"Tahsilat: {amount:N2} {Currency}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public AccountTransaction RecordReturn(Guid returnRequestId, decimal amount, PlatformType? platform = null)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.SalesReturn,
            DebitAmount = 0,
            CreditAmount = amount,
            ReturnRequestId = returnRequestId,
            Platform = platform,
            Description = $"Satış iadesi: {amount:N2} {Currency}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public AccountTransaction RecordCommission(Guid orderId, decimal amount, PlatformType platform)
    {
        var tx = new AccountTransaction
        {
            TenantId = TenantId,
            AccountId = Id,
            Type = TransactionType.PlatformCommission,
            DebitAmount = amount,
            CreditAmount = 0,
            OrderId = orderId,
            Platform = platform,
            Description = $"{platform} komisyon: {amount:N2} {Currency}"
        };
        _transactions.Add(tx);
        return tx;
    }

    public bool HasExceededCreditLimit => CreditLimit > 0 && Balance > CreditLimit;

    public override string ToString() => $"Müşteri [{AccountCode}] {CustomerName} Bakiye:{Balance:N2}";
}
