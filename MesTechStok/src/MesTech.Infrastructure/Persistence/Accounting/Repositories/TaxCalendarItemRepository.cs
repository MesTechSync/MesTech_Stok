using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class TaxCalendarItemRepository : ITaxCalendarItemRepository
{
    private readonly AppDbContext _context;
    public TaxCalendarItemRepository(AppDbContext context) => _context = context;

    public async Task<TaxCalendarItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TaxCalendarItems.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TaxCalendarItem>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.TaxCalendarItems
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DueMonth).ThenBy(x => x.DueDay)
            .Take(100)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(TaxCalendarItem item, CancellationToken ct = default)
        => await _context.TaxCalendarItems.AddAsync(item, ct).ConfigureAwait(false);

    public Task UpdateAsync(TaxCalendarItem item, CancellationToken ct = default)
    {
        _context.TaxCalendarItems.Update(item);
        return Task.CompletedTask;
    }
}
