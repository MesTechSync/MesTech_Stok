using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context) => _context = context;

    public async Task<StockMovement?> GetByIdAsync(Guid id)
        => await _context.StockMovements.FirstOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<StockMovement>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
        => await _context.StockMovements
            .Where(m => productIds.Contains(m.ProductId))
            .OrderBy(m => m.Date).ThenBy(m => m.CreatedAt)
            .Take(5000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId)
        => await _context.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Date)
            .Take(2000) // G485: pagination guard
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.StockMovements
            .Where(m => m.Date >= from && m.Date <= to)
            .OrderByDescending(m => m.Date)
            .Take(5000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<StockMovement>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default)
        => await _context.StockMovements
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Date)
            .Take(count)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(StockMovement movement)
        => await _context.StockMovements.AddAsync(movement).ConfigureAwait(false);

    public async Task<int> GetCountAsync(CancellationToken ct = default)
        => await _context.StockMovements.CountAsync(ct).ConfigureAwait(false);
}
