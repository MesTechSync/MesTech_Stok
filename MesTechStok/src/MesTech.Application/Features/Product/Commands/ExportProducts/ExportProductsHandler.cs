using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Product.Commands.ExportProducts;

/// <summary>
/// Ürün export handler'ı — Excel dosyası üretir.
/// </summary>
public sealed class ExportProductsHandler : IRequestHandler<ExportProductsCommand, byte[]>
{
    private readonly IBulkProductImportService _importService;

    public ExportProductsHandler(IBulkProductImportService importService)
    {
        _importService = importService;
    }

    public async Task<byte[]> Handle(
        ExportProductsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = new BulkExportOptions(
            request.Platform,
            request.CategoryId,
            request.InStock,
            request.Format);

        return await _importService.ExportProductsAsync(options, cancellationToken).ConfigureAwait(false);
    }
}
