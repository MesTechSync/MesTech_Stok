using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

public interface IExcelImportService
{
    Task<XmlImportResult> ImportProductsAsync(Stream excelStream, CancellationToken ct = default);
    Task<XmlImportResult> ImportStockAsync(Stream excelStream, CancellationToken ct = default);
    Task<XmlImportResult> ImportPricesAsync(Stream excelStream, CancellationToken ct = default);
}
