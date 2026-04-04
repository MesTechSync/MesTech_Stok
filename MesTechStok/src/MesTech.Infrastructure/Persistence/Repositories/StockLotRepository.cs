using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class StockLotRepository : IStockLotRepository
{
    private readonly AppDbContext _db;

    public StockLotRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockLot>> GetByTenantAsync(
        Guid tenantId, int limit = 50, CancellationToken ct = default)
    {
        return await _db.Set<StockLot>()
            .Include(l => l.Product)
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.ReceivedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StockLot>> GetByProductAsync(
        Guid tenantId, Guid productId, CancellationToken ct = default)
    {
        return await _db.Set<StockLot>()
            .Where(l => l.TenantId == tenantId && l.ProductId == productId && l.RemainingQuantity > 0)
            .OrderBy(l => l.ReceivedAt) // FIFO
            .Take(1000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<StockLot?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Set<StockLot>()
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);
    }

    public async Task AddAsync(StockLot lot, CancellationToken ct = default)
    {
        await _db.Set<StockLot>().AddAsync(lot, ct).ConfigureAwait(false);
    }
}
