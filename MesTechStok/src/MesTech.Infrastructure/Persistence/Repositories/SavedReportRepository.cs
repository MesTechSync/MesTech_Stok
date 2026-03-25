using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class SavedReportRepository : ISavedReportRepository
{
    private readonly AppDbContext _context;

    public SavedReportRepository(AppDbContext context) => _context = context;

    public async Task<SavedReport?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SavedReports
            .AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<SavedReport>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.SavedReports
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(SavedReport report, CancellationToken ct = default)
        => await _context.SavedReports.AddAsync(report, ct);

    public Task DeleteAsync(SavedReport report, CancellationToken ct = default)
    {
        _context.SavedReports.Remove(report);
        return Task.CompletedTask;
    }
}
