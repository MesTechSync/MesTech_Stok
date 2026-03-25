using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Cari hesap bakiye hesaplama domain servisi.
/// Balance = sum(debit) - sum(credit).
/// Saf iş kuralları, altyapı bağımlılığı yok.
/// </summary>
public sealed class BalanceCalculationService
{
    /// <summary>
    /// Hesap bakiyesi hesaplar: sum(debit) - sum(credit).
    /// </summary>
    public decimal CalculateBalance(IEnumerable<AccountTransaction> transactions)
    {
        return transactions.Sum(t => t.DebitAmount - t.CreditAmount);
    }

    /// <summary>
    /// Belirli bir tarihe kadar olan bakiye.
    /// </summary>
    public decimal CalculateBalanceAsOf(IEnumerable<AccountTransaction> transactions, DateTime asOf)
    {
        return transactions
            .Where(t => t.TransactionDate <= asOf)
            .Sum(t => t.DebitAmount - t.CreditAmount);
    }

    /// <summary>
    /// Vadesi geçmiş alacak/borç toplamı.
    /// </summary>
    public decimal CalculateOverdueAmount(IEnumerable<AccountTransaction> transactions, DateTime asOf)
    {
        return transactions
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < asOf)
            .Sum(t => t.DebitAmount - t.CreditAmount);
    }

    /// <summary>
    /// Hareket tipine göre toplam.
    /// </summary>
    public decimal CalculateTotalByType(IEnumerable<AccountTransaction> transactions, TransactionType type)
    {
        return transactions
            .Where(t => t.Type == type)
            .Sum(t => t.DebitAmount - t.CreditAmount);
    }

    /// <summary>
    /// Tarih aralığındaki hareket özeti.
    /// </summary>
    public AccountSummary CalculateSummary(IEnumerable<AccountTransaction> transactions, DateTime from, DateTime to)
    {
        var filtered = transactions
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .ToList();

        return new AccountSummary(
            TotalDebit: filtered.Sum(t => t.DebitAmount),
            TotalCredit: filtered.Sum(t => t.CreditAmount),
            NetBalance: filtered.Sum(t => t.DebitAmount - t.CreditAmount),
            TransactionCount: filtered.Count,
            From: from,
            To: to
        );
    }

    /// <summary>
    /// Platform bazlı komisyon toplamı.
    /// </summary>
    public decimal CalculatePlatformCommission(IEnumerable<AccountTransaction> transactions, PlatformType platform)
    {
        return transactions
            .Where(t => t.Type == TransactionType.PlatformCommission && t.Platform == platform)
            .Sum(t => t.DebitAmount);
    }
}

