using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IPlatformMessageRepository
{
    Task<PlatformMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<PlatformMessage> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, PlatformType? platform, MessageStatus? status,
        int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByStatusAsync(Guid tenantId, MessageStatus status, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(PlatformMessage message, CancellationToken ct = default);
}
