using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Dropshipping havuz veri erişim arayüzü.
/// </summary>
public interface IDropshippingPoolRepository
{
    Task<DropshippingPool?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<DropshippingPoolProduct> Items, int Total)> GetProductsPagedAsync(
        Guid tenantId,
        Guid? poolId,
        ReliabilityColor? colorFilter,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(IReadOnlyList<DropshippingPool> Items, int Total)> GetPoolsPagedAsync(
        Guid tenantId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DropshippingPoolProduct?> GetPoolProductByIdAsync(
        Guid poolProductId, CancellationToken ct = default);

    Task<IReadOnlyList<DropshippingPoolProduct>> GetPoolProductsByIdsAsync(
        Guid poolId, IEnumerable<Guid> productIds, CancellationToken ct = default);

    Task<PoolStats> GetStatsAsync(Guid tenantId, CancellationToken ct = default);

    Task AddAsync(DropshippingPool pool, CancellationToken ct = default);
    Task UpdateAsync(DropshippingPool pool, CancellationToken ct = default);
    Task AddPoolProductAsync(DropshippingPoolProduct poolProduct, CancellationToken ct = default);
    Task UpdatePoolProductAsync(DropshippingPoolProduct product, CancellationToken ct = default);
    Task RemovePoolProductAsync(DropshippingPoolProduct product, CancellationToken ct = default);
}

public record PoolStats(
    int Total, int Green, int Yellow, int Orange, int Red, decimal AverageScore);
