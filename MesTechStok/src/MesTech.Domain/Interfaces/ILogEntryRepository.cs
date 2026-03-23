using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ILogEntryRepository
{
    Task AddAsync(LogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<LogEntry>> GetPagedAsync(
        Guid tenantId,
        int page, int pageSize,
        string? category = null,
        string? userId = null,
        string? productName = null,
        string? barcode = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
    Task<long> GetCountAsync(Guid tenantId, string? category = null, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoffDate, CancellationToken ct = default);
}
