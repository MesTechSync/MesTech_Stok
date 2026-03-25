using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class PenaltyRecordRepository : IPenaltyRecordRepository
{
    private readonly AppDbContext _context;
    public PenaltyRecordRepository(AppDbContext context) => _context = context;

    public async Task<PenaltyRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PenaltyRecords.FindAsync([id], ct);

    public async Task<IReadOnlyList<PenaltyRecord>> GetAllAsync(Guid tenantId, PenaltySource? source = null, CancellationToken ct = default)
    {
        var query = _context.PenaltyRecords.Where(r => r.TenantId == tenantId);

        if (source.HasValue)
            query = query.Where(r => r.Source == source.Value);

        return await query
            .OrderByDescending(r => r.PenaltyDate)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(PenaltyRecord record, CancellationToken ct = default)
        => await _context.PenaltyRecords.AddAsync(record, ct);

    public Task UpdateAsync(PenaltyRecord record, CancellationToken ct = default)
    {
        _context.PenaltyRecords.Update(record);
        return Task.CompletedTask;
    }
}
