using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

public interface IXmlImportService
{
    Task<XmlImportResult> ImportProductsAsync(Stream xmlStream, CancellationToken ct = default);
    Task<XmlImportResult> ImportStockAsync(Stream xmlStream, CancellationToken ct = default);
    Task<XmlImportResult> ImportPricesAsync(Stream xmlStream, CancellationToken ct = default);
}
