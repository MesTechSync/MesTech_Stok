using MesTech.Domain.Entities.AI;

namespace MesTech.Domain.Interfaces;

public interface IStockPredictionRepository
{
    Task<StockPrediction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StockPrediction>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(StockPrediction prediction, CancellationToken ct = default);
}
