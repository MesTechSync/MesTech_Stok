using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(int id);
    Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(int productId);
    Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task AddAsync(StockMovement movement);
    Task<int> GetCountAsync();
}
