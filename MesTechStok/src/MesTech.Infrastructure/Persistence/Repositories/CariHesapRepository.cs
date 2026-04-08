using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CariHesapRepository : ICariHesapRepository
{
    private readonly AppDbContext _context;

    public CariHesapRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<CariHesap?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CariHesaplar
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHesap>> GetByTypeAsync(CariHesapType type, Guid? tenantId = null, CancellationToken ct = default)
        => await _context.CariHesaplar
            .Where(c => c.Type == type)
            .Where(c => tenantId == null || c.TenantId == tenantId.Value)
            .OrderBy(c => c.Name)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<CariHesap?> GetByNameAsync(Guid tenantId, string name, CancellationToken ct = default)
        => await _context.CariHesaplar
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHesap>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default)
        => await _context.CariHesaplar
            .Where(c => tenantId == null || c.TenantId == tenantId.Value)
            .OrderBy(c => c.Name)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(CariHesap cariHesap, CancellationToken ct = default)
        => await _context.CariHesaplar.AddAsync(cariHesap, ct).ConfigureAwait(false);

    public Task UpdateAsync(CariHesap cariHesap, CancellationToken ct = default)
    {
        _context.CariHesaplar.Update(cariHesap);
        return Task.CompletedTask;
    }
}
