using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly AppDbContext _context;

    public PriceHistoryRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(PriceHistory priceHistory, CancellationToken ct = default)
        => await _context.PriceHistories.AddAsync(priceHistory, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<PriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.PriceHistories
            .Where(ph => ph.ProductId == productId)
            .OrderByDescending(ph => ph.ChangedAt)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
