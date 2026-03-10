using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

public interface IExcelExportService
{
    Task<Stream> ExportProductsAsync(IEnumerable<ProductExportDto> products, CancellationToken ct = default);
    Task<Stream> ExportOrdersAsync(IEnumerable<OrderExportDto> orders, CancellationToken ct = default);
    Task<Stream> ExportStockAsync(IEnumerable<StockExportDto> items, CancellationToken ct = default);
    Task<Stream> ExportProfitabilityAsync(IEnumerable<ProfitabilityExportDto> items, CancellationToken ct = default);
}
