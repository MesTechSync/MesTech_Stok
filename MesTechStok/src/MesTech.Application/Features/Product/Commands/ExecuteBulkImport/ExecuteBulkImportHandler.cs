using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Product.Commands.ExecuteBulkImport;

/// <summary>
/// Toplu ürün import handler'ı.
/// Validasyon + import pipeline'ını orchestrate eder.
/// </summary>
public class ExecuteBulkImportHandler : IRequestHandler<ExecuteBulkImportCommand, ImportResult>
{
    private readonly IBulkProductImportService _importService;

    public ExecuteBulkImportHandler(IBulkProductImportService importService)
    {
        _importService = importService;
    }

    public async Task<ImportResult> Handle(
        ExecuteBulkImportCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileStream is null || request.FileStream.Length == 0)
        {
            return new ImportResult(
                ImportStatus.Failed, 0, 0, 0, 0, 1,
                [new ImportRowError(0, "File", "Dosya boş veya geçersiz.")],
                TimeSpan.Zero);
        }

        var extension = Path.GetExtension(request.FileName)?.ToUpperInvariant();
        if (extension is not ".XLSX" and not ".XLS")
        {
            return new ImportResult(
                ImportStatus.Failed, 0, 0, 0, 0, 1,
                [new ImportRowError(0, "File", "Sadece .xlsx ve .xls dosyaları desteklenir.")],
                TimeSpan.Zero);
        }

        var options = new ImportOptions(
            request.UpdateExisting,
            request.SkipErrors,
            request.TargetPlatform,
            request.CategoryId);

        return await _importService.ImportProductsAsync(
            request.FileStream, options, cancellationToken);
    }
}
