using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class DealRepository : IDealRepository
{
    private readonly AppDbContext _context;

    public DealRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Deal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);

    public async Task<Deal?> GetByIdTrackedWithContactAsync(Guid id, CancellationToken ct = default)
        => await _context.Deals
            .Include(d => d.Contact)
            .FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Deal>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Deals
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(5000) // G560: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Deal deal, CancellationToken ct = default)
        => await _context.Deals.AddAsync(deal, ct).ConfigureAwait(false);

    public Task UpdateAsync(Deal deal, CancellationToken ct = default)
    {
        _context.Deals.Update(deal);
        return Task.CompletedTask;
    }
}
