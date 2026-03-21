using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class BarcodeScanLogRepository : IBarcodeScanLogRepository
{
    private readonly AppDbContext _context;

    public BarcodeScanLogRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<BarcodeScanLog?> GetByIdAsync(Guid id)
        => await _context.BarcodeScanLogs.FindAsync(id).ConfigureAwait(false);

    public async Task<IReadOnlyList<BarcodeScanLog>> GetPagedAsync(
        int page, int pageSize,
        string? barcodeFilter, string? sourceFilter,
        bool? isValidFilter, DateTime? from, DateTime? to)
    {
        var query = BuildFilteredQuery(barcodeFilter, sourceFilter, isValidFilter, from, to);
        return await query
            .OrderByDescending(l => l.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<int> GetCountAsync(
        string? barcodeFilter, string? sourceFilter,
        bool? isValidFilter, DateTime? from, DateTime? to)
    {
        return await BuildFilteredQuery(barcodeFilter, sourceFilter, isValidFilter, from, to)
            .CountAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(BarcodeScanLog log)
        => await _context.BarcodeScanLogs.AddAsync(log).ConfigureAwait(false);

    private IQueryable<BarcodeScanLog> BuildFilteredQuery(
        string? barcodeFilter, string? sourceFilter,
        bool? isValidFilter, DateTime? from, DateTime? to)
    {
        var query = _context.BarcodeScanLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(barcodeFilter))
            query = query.Where(l => l.Barcode.Contains(barcodeFilter));
        if (!string.IsNullOrWhiteSpace(sourceFilter))
            query = query.Where(l => l.Source == sourceFilter);
        if (isValidFilter.HasValue)
            query = query.Where(l => l.IsValid == isValidFilter.Value);
        if (from.HasValue)
            query = query.Where(l => l.TimestampUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.TimestampUtc <= to.Value);

        return query;
    }
}
