using MesTech.Domain.Entities.AI;

namespace MesTech.Domain.Interfaces;

public interface IPriceRecommendationRepository
{
    Task<PriceRecommendation?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<PriceRecommendation>> GetByProductIdAsync(Guid productId);
    Task AddAsync(PriceRecommendation recommendation);
}
