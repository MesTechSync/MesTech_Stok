using MesTech.Domain.Entities.AI;

namespace MesTech.Domain.Interfaces;

public interface IPriceRecommendationRepository
{
    Task<PriceRecommendation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PriceRecommendation>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(PriceRecommendation recommendation, CancellationToken ct = default);
}
