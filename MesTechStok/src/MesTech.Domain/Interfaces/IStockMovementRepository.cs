using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId);
    Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IReadOnlyList<StockMovement>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default);
    Task AddAsync(StockMovement movement);
    Task<int> GetCountAsync();
}
