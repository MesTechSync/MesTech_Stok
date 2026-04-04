using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Warehouse?> GetByIdAsync(Guid id)
        => await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default)
        => await _context.Warehouses.Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<Warehouse?> GetDefaultAsync()
        => await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.IsDefault).ConfigureAwait(false);

    public async Task AddAsync(Warehouse warehouse)
        => await _context.Warehouses.AddAsync(warehouse).ConfigureAwait(false);

    public Task UpdateAsync(Warehouse warehouse)
    {
        _context.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var wh = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id).ConfigureAwait(false);
        if (wh != null) _context.Warehouses.Remove(wh);
    }
}
