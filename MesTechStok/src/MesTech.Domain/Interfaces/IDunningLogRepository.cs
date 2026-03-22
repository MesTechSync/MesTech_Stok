using MesTech.Domain.Entities.Billing;

namespace MesTech.Domain.Interfaces;

public interface IDunningLogRepository
{
    Task<IReadOnlyList<DunningLog>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken ct = default);
    Task<int> GetAttemptCountAsync(Guid subscriptionId, CancellationToken ct = default);
    Task AddAsync(DunningLog log, CancellationToken ct = default);
}
