using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default);
    Task AddAsync(StockMovement movement, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}
