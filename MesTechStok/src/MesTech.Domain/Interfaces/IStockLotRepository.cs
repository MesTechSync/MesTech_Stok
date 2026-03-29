using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IStockLotRepository
{
    Task<IReadOnlyList<StockLot>> GetByTenantAsync(Guid tenantId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<StockLot>> GetByProductAsync(Guid tenantId, Guid productId, CancellationToken ct = default);
    Task<StockLot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(StockLot lot, CancellationToken ct = default);
}
