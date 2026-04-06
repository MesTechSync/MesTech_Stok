namespace MesTech.Application.Interfaces;

/// <summary>
/// Report generation service — NOT YET IMPLEMENTED.
/// No implementation exists, no DI registration. Injecting this will cause runtime DI failure.
/// Planned for future sprint — stock/movement/inventory value PDF/Excel export.
/// </summary>
[System.Obsolete("Not implemented — no DI registration. Will throw at runtime if injected. Planned for future sprint.")]
public interface IReportService
{
    Task<byte[]> GenerateStockReportAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<byte[]> GenerateMovementReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<byte[]> GenerateInventoryValueReportAsync(CancellationToken ct = default);
}
