using Hangfire;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Crm;

/// <summary>
/// Daily job (02:00) — expires loyalty Earn transactions older than 12 months.
/// Creates an Expire transaction for each expirable Earn, zeroing out the points.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class ExpirePointsWorker : ISyncJob
{
    public string JobId => "crm-expire-loyalty-points";
    public string CronExpression => "0 2 * * *"; // Daily at 02:00

    private static readonly TimeSpan ExpiryThreshold = TimeSpan.FromDays(365);

    private readonly ILoyaltyTransactionRepository _transactionRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExpirePointsWorker> _logger;

    public ExpirePointsWorker(
        ILoyaltyTransactionRepository transactionRepo,
        IUnitOfWork uow,
        ILogger<ExpirePointsWorker> logger)
    {
        _transactionRepo = transactionRepo;
        _uow = uow;
        _logger = logger;
    }

    [DisableConcurrentExecution(120)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Loyalty points expiration starting...", JobId);

        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(ExpiryThreshold);

            // Find all Earn transactions older than 12 months that haven't been expired
            var expirableTransactions = await _transactionRepo
                .GetExpirableEarnTransactionsAsync(cutoffDate, ct)
                .ConfigureAwait(false);

            if (expirableTransactions.Count == 0)
            {
                _logger.LogInformation("[{JobId}] No expirable transactions found", JobId);
                return;
            }

            var expiredCount = 0;
            var totalExpiredPoints = 0;

            foreach (var earnTx in expirableTransactions)
            {
                ct.ThrowIfCancellationRequested();

                // Create Expire transaction to zero out these points
                var expireTx = LoyaltyTransaction.Create(
                    earnTx.TenantId,
                    earnTx.CustomerId,
                    earnTx.LoyaltyProgramId,
                    -earnTx.Points,
                    LoyaltyTransactionType.Expire,
                    $"Expired {earnTx.Points} points from transaction {earnTx.Id}");

                await _transactionRepo.AddAsync(expireTx, ct).ConfigureAwait(false);
                expiredCount++;
                totalExpiredPoints += earnTx.Points;
            }

            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Expiration complete: {Count} transactions, {Points} total points expired",
                JobId, expiredCount, totalExpiredPoints);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Loyalty points expiration ERROR", JobId);
            throw;
        }
    }
}
