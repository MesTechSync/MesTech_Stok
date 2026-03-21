using MesTech.Domain.Entities.Billing;

namespace MesTech.Domain.Interfaces;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionPlan>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default);
}
