using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;

    public SupplierRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id)
    {
        return await _context.Suppliers.FindAsync(id);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers
            .OrderBy(s => s.Name)
            .AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<Supplier>> GetActiveAsync()
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Suppliers.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
        }
    }
}
