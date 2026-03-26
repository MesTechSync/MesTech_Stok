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

    public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId)
        => await _context.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Date)
            .AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await _context.StockMovements
            .Where(m => m.Date >= from && m.Date <= to)
            .OrderByDescending(m => m.Date)
            .AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<StockMovement>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default)
        => await _context.StockMovements
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Date)
            .Take(count)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(StockMovement movement)
        => await _context.StockMovements.AddAsync(movement);

    public async Task<int> GetCountAsync()
        => await _context.StockMovements.CountAsync();
}
