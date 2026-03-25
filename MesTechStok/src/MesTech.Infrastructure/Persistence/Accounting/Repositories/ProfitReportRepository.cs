using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class ProfitReportRepository : IProfitReportRepository
{
    private readonly AppDbContext _context;
    public ProfitReportRepository(AppDbContext context) => _context = context;

    public async Task<ProfitReport?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ProfitReports.FindAsync([id], ct);

    public async Task<IReadOnlyList<ProfitReport>> GetByPeriodAsync(Guid tenantId, string period, string? platform = null, CancellationToken ct = default)
    {
        var q = _context.ProfitReports
            .Where(r => r.TenantId == tenantId && r.Period == period);
        if (!string.IsNullOrWhiteSpace(platform))
            q = q.Where(r => r.Platform == platform);
        return await q.OrderByDescending(r => r.ReportDate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<ProfitReport?> GetLatestAsync(Guid tenantId, string? platform = null, CancellationToken ct = default)
    {
        var q = _context.ProfitReports.Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(platform))
            q = q.Where(r => r.Platform == platform);
        return await q.OrderByDescending(r => r.ReportDate).AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(ProfitReport report, CancellationToken ct = default)
        => await _context.ProfitReports.AddAsync(report, ct);
}
