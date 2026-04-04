using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;

    public SupplierRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Suppliers
            .OrderBy(s => s.Name)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken ct = default)
    {
        await _context.Suppliers.AddAsync(supplier, ct).ConfigureAwait(false);
    }

    public Task UpdateAsync(Supplier supplier, CancellationToken ct = default)
    {
        _context.Suppliers.Update(supplier);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (entity != null)
        {
            entity.IsDeleted = true;
        }
    }
}
