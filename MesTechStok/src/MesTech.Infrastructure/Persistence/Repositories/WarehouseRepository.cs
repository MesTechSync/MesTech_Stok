using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default)
        => await _context.Warehouses.Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default)
        => await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.IsDefault, ct).ConfigureAwait(false);

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
        => await _context.Warehouses.AddAsync(warehouse, ct).ConfigureAwait(false);

    public Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        _context.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var wh = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct).ConfigureAwait(false);
        if (wh != null) _context.Warehouses.Remove(wh);
    }
}
