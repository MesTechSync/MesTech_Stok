using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBarcodeScanLogRepository
{
    Task<BarcodeScanLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BarcodeScanLog>> GetPagedAsync(
        int page, int pageSize,
        string? barcodeFilter = null,
        string? sourceFilter = null,
        bool? isValidFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
    Task<int> GetCountAsync(
        string? barcodeFilter = null,
        string? sourceFilter = null,
        bool? isValidFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
    Task AddAsync(BarcodeScanLog log, CancellationToken ct = default);
}
