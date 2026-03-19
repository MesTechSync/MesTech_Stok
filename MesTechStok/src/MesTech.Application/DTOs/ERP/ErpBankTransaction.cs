namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP banka hareketi bilgisi.
/// </summary>
public record ErpBankTransaction(
    DateTime TransactionDate,
    decimal Amount,
    string Description,
    string TransactionType,
    string? Reference
);
