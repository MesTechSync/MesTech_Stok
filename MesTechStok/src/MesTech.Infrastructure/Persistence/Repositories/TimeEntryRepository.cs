using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _context;

    public TimeEntryRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<TimeEntry>> GetByTenantAsync(
        Guid tenantId, DateTime from, DateTime to, Guid? userId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _context.Set<TimeEntry>()
            .Where(e => e.TenantId == tenantId && e.StartedAt >= from && e.StartedAt <= to);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        return await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<TimeEntry>().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task AddAsync(TimeEntry entry, CancellationToken ct = default)
        => await _context.Set<TimeEntry>().AddAsync(entry, ct).ConfigureAwait(false);

    public Task UpdateAsync(TimeEntry entry, CancellationToken ct = default)
    {
        _context.Set<TimeEntry>().Update(entry);
        return Task.CompletedTask;
    }
}
