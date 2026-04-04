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

    public async Task<Supplier?> GetByIdAsync(Guid id)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
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

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
        if (entity != null)
        {
            entity.IsDeleted = true;
        }
    }
}
