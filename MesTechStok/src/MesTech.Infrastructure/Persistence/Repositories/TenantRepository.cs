using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default)
        => await _context.Tenants.Where(t => t.IsActive).AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<Tenant?> GetByTaxNumberAsync(string taxNumber, CancellationToken ct = default)
        => await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TaxNumber == taxNumber, ct).ConfigureAwait(false);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => await _context.Tenants.AddAsync(tenant, ct).ConfigureAwait(false);

    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _context.Tenants.Update(tenant);
        return Task.CompletedTask;
    }
}
