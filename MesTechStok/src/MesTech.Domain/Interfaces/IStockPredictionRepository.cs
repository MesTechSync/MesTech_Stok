using MesTech.Domain.Entities.AI;

namespace MesTech.Domain.Interfaces;

public interface IStockPredictionRepository
{
    Task<StockPrediction?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<StockPrediction>> GetByProductIdAsync(Guid productId);
    Task AddAsync(StockPrediction prediction);
}
