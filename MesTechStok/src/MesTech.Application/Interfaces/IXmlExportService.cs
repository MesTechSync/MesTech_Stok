using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

public interface IXmlExportService
{
    Task<Stream> ExportProductsAsync(IEnumerable<ProductExportDto> products, CancellationToken ct = default);
    Task<Stream> ExportStockAsync(IEnumerable<StockExportDto> items, CancellationToken ct = default);
    Task<Stream> ExportPricesAsync(IEnumerable<PriceExportDto> items, CancellationToken ct = default);
}
