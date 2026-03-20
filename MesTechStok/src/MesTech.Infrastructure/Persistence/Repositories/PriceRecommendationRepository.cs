using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class PriceRecommendationRepository : IPriceRecommendationRepository
{
    private readonly AppDbContext _context;

    public PriceRecommendationRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<PriceRecommendation?> GetByIdAsync(Guid id)
        => await _context.PriceRecommendations.FindAsync(id).ConfigureAwait(false);

    public async Task<IReadOnlyList<PriceRecommendation>> GetByProductIdAsync(Guid productId)
        => await _context.PriceRecommendations
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task AddAsync(PriceRecommendation recommendation)
        => await _context.PriceRecommendations.AddAsync(recommendation).ConfigureAwait(false);
}
