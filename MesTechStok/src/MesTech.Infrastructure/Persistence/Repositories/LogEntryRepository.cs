using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class LogEntryRepository : ILogEntryRepository
{
    private readonly AppDbContext _db;

    public LogEntryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(LogEntry entry, CancellationToken ct = default)
    {
        await _db.Set<LogEntry>().AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<LogEntry>> GetPagedAsync(
        Guid tenantId,
        int page, int pageSize,
        string? category = null,
        string? userId = null,
        string? productName = null,
        string? barcode = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = _db.Set<LogEntry>()
            .Where(e => e.TenantId == tenantId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(e => e.UserId == userId);

        if (!string.IsNullOrWhiteSpace(productName))
            query = query.Where(e => e.Category == "Product" && e.Message.Contains(productName));

        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(e => e.Category == "Product" && e.Message.Contains(barcode));

        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<long> GetCountAsync(Guid tenantId, string? category = null, CancellationToken ct = default)
    {
        var query = _db.Set<LogEntry>()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        return await query.LongCountAsync(ct);
    }

    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoffDate, CancellationToken ct = default)
    {
        return await _db.Set<LogEntry>()
            .Where(e => e.TenantId == tenantId && e.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(ct);
    }
}
