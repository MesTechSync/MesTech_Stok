using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Stores
            .Include(s => s.Credentials)
            .Include(s => s.ProductMappings)
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Store>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Stores
            .Include(s => s.ProductMappings)
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Store>> GetByPlatformTypeAsync(PlatformType platformType, CancellationToken ct = default)
        => await _context.Stores
            .Where(s => s.PlatformType == platformType && s.IsActive)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Store store, CancellationToken ct = default)
        => await _context.Stores.AddAsync(store, ct);

    public Task UpdateAsync(Store store, CancellationToken ct = default)
    {
        _context.Stores.Update(store);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Store store, CancellationToken ct = default)
    {
        _context.Stores.Remove(store);
        return Task.CompletedTask;
    }
}
