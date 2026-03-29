using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ISocialFeedConfigurationRepository
{
    Task<IReadOnlyList<SocialFeedConfiguration>> GetActiveAsync(CancellationToken ct = default);
    Task<SocialFeedConfiguration?> GetByTenantAndPlatformAsync(
        Guid tenantId, SocialFeedPlatform platform, CancellationToken ct = default);
    Task UpdateAsync(SocialFeedConfiguration config, CancellationToken ct = default);
}
