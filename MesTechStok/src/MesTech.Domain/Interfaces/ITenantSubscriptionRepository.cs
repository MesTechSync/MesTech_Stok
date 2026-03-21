using MesTech.Domain.Entities.Billing;

namespace MesTech.Domain.Interfaces;

public interface ITenantSubscriptionRepository
{
    Task<TenantSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantSubscription?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSubscription>> GetExpiringAsync(int withinDays, CancellationToken ct = default);
    Task AddAsync(TenantSubscription subscription, CancellationToken ct = default);
    Task UpdateAsync(TenantSubscription subscription, CancellationToken ct = default);
}
