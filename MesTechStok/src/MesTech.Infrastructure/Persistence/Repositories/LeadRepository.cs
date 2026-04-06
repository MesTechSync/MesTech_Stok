using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _context;

    public LeadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Lead>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Leads
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<bool> AnyByTenantAndNameAsync(Guid tenantId, string name, CancellationToken ct = default)
        => await _context.Leads
            .AnyAsync(l => l.TenantId == tenantId && l.FullName == name, ct).ConfigureAwait(false);

    public async Task AddAsync(Lead lead, CancellationToken ct = default)
        => await _context.Leads.AddAsync(lead, ct).ConfigureAwait(false);

    public Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Update(lead);
        return Task.CompletedTask;
    }
}
