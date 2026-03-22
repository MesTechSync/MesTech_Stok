using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ILoyaltyTransactionRepository
{
    Task<IReadOnlyList<LoyaltyTransaction>> GetByCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken ct = default);

    Task<IReadOnlyList<LoyaltyTransaction>> GetByCustomerPagedAsync(
        Guid tenantId, Guid customerId, int take, CancellationToken ct = default);

    Task<int> GetPointsSumByTypeAsync(
        Guid tenantId, Guid customerId, LoyaltyTransactionType type, CancellationToken ct = default);

    /// <summary>
    /// Returns Earn transactions older than the given date that have not been expired.
    /// </summary>
    Task<IReadOnlyList<LoyaltyTransaction>> GetExpirableEarnTransactionsAsync(
        DateTime olderThan, CancellationToken ct = default);

    Task AddAsync(LoyaltyTransaction transaction, CancellationToken ct = default);
}
