using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Paraşüt muhasebe entegrasyon servisi.
/// Gelir/gider kayıtlarını Paraşüt'e iletir; bakiye ve hareketleri çeker.
/// </summary>
public interface IParasutAccountingService
{
    /// <summary>Push an income record to Paraşüt as a sales invoice.</summary>
    Task<ParasutSyncResult> PushIncomeAsync(Guid incomeId, CancellationToken ct = default);

    /// <summary>Push an expense record to Paraşüt as a purchase invoice.</summary>
    Task<ParasutSyncResult> PushExpenseAsync(Guid expenseId, CancellationToken ct = default);

    /// <summary>Pull current account balance from Paraşüt.</summary>
    Task<ParasutBalanceDto> GetBalanceAsync(CancellationToken ct = default);

    /// <summary>Pull recent transactions from Paraşüt (last N days).</summary>
    Task<IReadOnlyList<ParasutTransactionDto>> GetRecentTransactionsAsync(int days = 30, CancellationToken ct = default);
}
