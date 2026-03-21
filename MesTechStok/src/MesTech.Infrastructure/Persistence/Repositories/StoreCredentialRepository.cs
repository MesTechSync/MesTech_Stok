using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core StoreCredential repository implementasyonu.
/// Soft-delete filtresi AppDbContext global query filter'da uygulanir.
/// </summary>
public class StoreCredentialRepository : IStoreCredentialRepository
{
    private readonly AppDbContext _context;

    public StoreCredentialRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StoreCredential?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<StoreCredential>()
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<StoreCredential>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default)
    {
        return await _context.Set<StoreCredential>()
            .Where(c => c.StoreId == storeId && !c.IsDeleted)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(StoreCredential credential, CancellationToken ct = default)
    {
        await _context.Set<StoreCredential>().AddAsync(credential, ct);
    }

    public async Task UpdateAsync(StoreCredential credential, CancellationToken ct = default)
    {
        credential.UpdatedAt = DateTime.UtcNow;
        _context.Set<StoreCredential>().Update(credential);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(StoreCredential credential, CancellationToken ct = default)
    {
        credential.IsDeleted = true;
        credential.DeletedAt = DateTime.UtcNow;
        _context.Set<StoreCredential>().Update(credential);
        await Task.CompletedTask;
    }
}
