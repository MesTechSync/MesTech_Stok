using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class CommissionRecordRepository : ICommissionRecordRepository
{
    private readonly AppDbContext _context;
    public CommissionRecordRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<CommissionRecord>> GetByPlatformAsync(Guid tenantId, string platform, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _context.CommissionRecords
            .Where(r => r.TenantId == tenantId && r.Platform == platform);
        if (from.HasValue) q = q.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.CreatedAt <= to.Value);
        return await q.OrderByDescending(r => r.CreatedAt).AsNoTracking().ToListAsync(ct);
    }

    public async Task<decimal> GetTotalCommissionAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CommissionRecords
            .Where(r => r.TenantId == tenantId && r.CreatedAt >= from && r.CreatedAt <= to)
            .SumAsync(r => r.CommissionAmount, ct);

    public async Task AddAsync(CommissionRecord record, CancellationToken ct = default)
        => await _context.CommissionRecords.AddAsync(record, ct);
}
