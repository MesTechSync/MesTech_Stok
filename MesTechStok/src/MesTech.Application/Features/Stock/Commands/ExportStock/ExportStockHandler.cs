using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stock.Commands.ExportStock;

/// <summary>
/// Stok dışa aktarma handler'ı — IExcelExportService ile gerçek Excel dosyası üretir.
/// </summary>
public sealed class ExportStockHandler : IRequestHandler<ExportStockCommand, ExportStockResult>
{
    private readonly IProductRepository _productRepo;
    private readonly IExcelExportService _excelService;
    private readonly ILogger<ExportStockHandler> _logger;

    public ExportStockHandler(
        IProductRepository productRepo,
        IExcelExportService excelService,
        ILogger<ExportStockHandler> logger)
    {
        _productRepo = productRepo;
        _excelService = excelService;
        _logger = logger;
    }

    public async Task<ExportStockResult> Handle(
        ExportStockCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var products = await _productRepo.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var stockItems = products.Select(p => new StockExportDto(p.SKU, p.Name, p.Stock)).ToList();

        _logger.LogInformation("Stok export başlıyor — {Count} ürün, Format={Format}",
            stockItems.Count, request.Format);

        using var stream = await _excelService.ExportStockAsync(stockItems, cancellationToken)
            .ConfigureAwait(false);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var bytes = ms.ToArray();

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        _logger.LogInformation("Stok export tamamlandı — {Count} ürün, {Size} byte",
            stockItems.Count, bytes.Length);

        return new ExportStockResult
        {
            FileData = bytes,
            FileName = $"stok_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}",
            ExportedCount = stockItems.Count
        };
    }
}
