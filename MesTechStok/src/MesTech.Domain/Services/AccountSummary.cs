namespace MesTech.Domain.Services;

/// <summary>
/// Hesap özeti sonucu.
/// </summary>
public record AccountSummary(
    decimal TotalDebit,
    decimal TotalCredit,
    decimal NetBalance,
    int TransactionCount,
    DateTime From,
    DateTime To
);
