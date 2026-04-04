using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Hr;

public sealed class LeaveRepository : ILeaveRepository
{
    private readonly AppDbContext _context;
    public LeaveRepository(AppDbContext context) => _context = context;

    public async Task<Leave?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Leaves.Include(l => l.Employee).AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Leave>> GetByTenantAsync(
        Guid tenantId, LeaveStatus? status = null, CancellationToken ct = default)
    {
        var q = _context.Leaves.Include(l => l.Employee).Where(l => l.TenantId == tenantId);
        if (status.HasValue) q = q.Where(l => l.Status == status.Value);
        return await q.OrderByDescending(l => l.CreatedAt).Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Leave>> GetCurrentMonthAsync(Guid tenantId, CancellationToken ct = default)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await _context.Leaves
            .Where(l => l.TenantId == tenantId
                     && l.Status == LeaveStatus.Approved
                     && l.StartDate <= end && l.EndDate >= start)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(Leave leave, CancellationToken ct = default)
        => await _context.Leaves.AddAsync(leave, ct).ConfigureAwait(false);
}
