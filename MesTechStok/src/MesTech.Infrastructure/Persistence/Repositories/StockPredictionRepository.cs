using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class StockPredictionRepository : IStockPredictionRepository
{
    private readonly AppDbContext _context;

    public StockPredictionRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<StockPrediction?> GetByIdAsync(Guid id)
        => await _context.StockPredictions.FindAsync(id).ConfigureAwait(false);

    public async Task<IReadOnlyList<StockPrediction>> GetByProductIdAsync(Guid productId)
        => await _context.StockPredictions
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);

    public async Task AddAsync(StockPrediction prediction)
        => await _context.StockPredictions.AddAsync(prediction).ConfigureAwait(false);
}
