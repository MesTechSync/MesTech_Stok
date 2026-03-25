using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CariHesapRepository : ICariHesapRepository
{
    private readonly AppDbContext _context;

    public CariHesapRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<CariHesap?> GetByIdAsync(Guid id)
        => await _context.CariHesaplar
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHesap>> GetByTypeAsync(CariHesapType type, Guid? tenantId = null)
        => await _context.CariHesaplar
            .Where(c => c.Type == type)
            .Where(c => tenantId == null || c.TenantId == tenantId.Value)
            .OrderBy(c => c.Name)
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHesap>> GetAllAsync(Guid? tenantId = null)
        => await _context.CariHesaplar
            .Where(c => tenantId == null || c.TenantId == tenantId.Value)
            .OrderBy(c => c.Name)
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(CariHesap cariHesap)
        => await _context.CariHesaplar.AddAsync(cariHesap).ConfigureAwait(false);

    public Task UpdateAsync(CariHesap cariHesap)
    {
        _context.CariHesaplar.Update(cariHesap);
        return Task.CompletedTask;
    }
}
