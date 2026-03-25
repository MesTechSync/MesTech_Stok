using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class SalaryRecordRepository : ISalaryRecordRepository
{
    private readonly AppDbContext _context;
    public SalaryRecordRepository(AppDbContext context) => _context = context;

    public async Task<SalaryRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SalaryRecords.FindAsync([id], ct);

    public async Task<IReadOnlyList<SalaryRecord>> GetAllAsync(Guid tenantId, int? year = null, int? month = null, CancellationToken ct = default)
    {
        var query = _context.SalaryRecords.Where(r => r.TenantId == tenantId);

        if (year.HasValue)
            query = query.Where(r => r.Year == year.Value);
        if (month.HasValue)
            query = query.Where(r => r.Month == month.Value);

        return await query
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(SalaryRecord record, CancellationToken ct = default)
        => await _context.SalaryRecords.AddAsync(record, ct);

    public Task UpdateAsync(SalaryRecord record, CancellationToken ct = default)
    {
        _context.SalaryRecords.Update(record);
        return Task.CompletedTask;
    }
}
