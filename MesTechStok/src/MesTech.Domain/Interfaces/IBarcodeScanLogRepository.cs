using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBarcodeScanLogRepository
{
    Task<BarcodeScanLog?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<BarcodeScanLog>> GetPagedAsync(
        int page, int pageSize,
        string? barcodeFilter = null,
        string? sourceFilter = null,
        bool? isValidFilter = null,
        DateTime? from = null,
        DateTime? to = null);
    Task<int> GetCountAsync(
        string? barcodeFilter = null,
        string? sourceFilter = null,
        bool? isValidFilter = null,
        DateTime? from = null,
        DateTime? to = null);
    Task AddAsync(BarcodeScanLog log);
}
