using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class CounterpartyRepository : ICounterpartyRepository
{
    private readonly AppDbContext _context;
    public CounterpartyRepository(AppDbContext context) => _context = context;

    public async Task<Counterparty?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Counterparties.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Counterparty>> GetAllAsync(Guid tenantId, CounterpartyType? type = null, bool? isActive = null, CancellationToken ct = default)
    {
        var q = _context.Counterparties.Where(c => c.TenantId == tenantId);
        if (type.HasValue) q = q.Where(c => c.CounterpartyType == type.Value);
        if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
        return await q.OrderBy(c => c.Name).Take(1000).AsNoTracking().ToListAsync(ct); // G485: pagination guard
    }

    public async Task<Counterparty?> GetByVknAsync(Guid tenantId, string vkn, CancellationToken ct = default)
        => await _context.Counterparties
            .AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId && c.VKN == vkn, ct);

    public async Task AddAsync(Counterparty counterparty, CancellationToken ct = default)
        => await _context.Counterparties.AddAsync(counterparty, ct);

    public Task UpdateAsync(Counterparty counterparty, CancellationToken ct = default)
    {
        _context.Counterparties.Update(counterparty);
        return Task.CompletedTask;
    }
}
