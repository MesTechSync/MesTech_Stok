using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Tedarikçi feed veri erişim arayüzü.
/// </summary>
public interface ISupplierFeedRepository
{
    Task<SupplierFeed?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<SupplierFeed> Items, int Total)> GetPagedAsync(
        Guid tenantId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetActiveCountAsync(Guid tenantId, CancellationToken ct = default);
    Task<DateTime?> GetLastSyncAtAsync(Guid tenantId, CancellationToken ct = default);

    Task AddAsync(SupplierFeed feed, CancellationToken ct = default);
    Task UpdateAsync(SupplierFeed feed, CancellationToken ct = default);
}
