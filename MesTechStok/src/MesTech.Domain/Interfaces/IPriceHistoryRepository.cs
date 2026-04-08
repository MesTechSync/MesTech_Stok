using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IPriceHistoryRepository
{
    Task AddAsync(PriceHistory priceHistory, CancellationToken ct = default);
    Task<IReadOnlyList<PriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
}
