namespace MesTech.Application.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateStockReportAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<byte[]> GenerateMovementReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<byte[]> GenerateInventoryValueReportAsync(CancellationToken ct = default);
}
