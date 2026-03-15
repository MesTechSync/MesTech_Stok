using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class TaxRecordRepository : ITaxRecordRepository
{
    private readonly AppDbContext _context;
    public TaxRecordRepository(AppDbContext context) => _context = context;

    public async Task<TaxRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TaxRecords.FindAsync([id], ct);

    public async Task<IReadOnlyList<TaxRecord>> GetByPeriodAsync(Guid tenantId, string period, CancellationToken ct = default)
        => await _context.TaxRecords
            .Where(r => r.TenantId == tenantId && r.Period == period)
            .OrderBy(r => r.DueDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<TaxRecord>> GetUnpaidAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.TaxRecords
            .Where(r => r.TenantId == tenantId && !r.IsPaid)
            .OrderBy(r => r.DueDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<decimal> GetTotalTaxByPeriodAsync(Guid tenantId, string period, CancellationToken ct = default)
        => await _context.TaxRecords
            .Where(r => r.TenantId == tenantId && r.Period == period)
            .SumAsync(r => r.TaxAmount, ct);

    public async Task AddAsync(TaxRecord record, CancellationToken ct = default)
        => await _context.TaxRecords.AddAsync(record, ct);

    public Task UpdateAsync(TaxRecord record, CancellationToken ct = default)
    {
        _context.TaxRecords.Update(record);
        return Task.CompletedTask;
    }
}
