using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Product.Commands.ValidateBulkImport;

/// <summary>
/// Excel dosyası validasyon handler'ı.
/// </summary>
public class ValidateBulkImportHandler : IRequestHandler<ValidateBulkImportCommand, ImportValidationResult>
{
    private readonly IBulkProductImportService _importService;

    public ValidateBulkImportHandler(IBulkProductImportService importService)
    {
        _importService = importService;
    }

    public async Task<ImportValidationResult> Handle(
        ValidateBulkImportCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.FileStream is null || request.FileStream.Length == 0)
        {
            return new ImportValidationResult(
                false, 0, 0, 0,
                [new ImportRowError(0, "File", "Dosya boş veya geçersiz.")]);
        }

        var extension = Path.GetExtension(request.FileName)?.ToUpperInvariant();
        if (extension is not ".XLSX" and not ".XLS")
        {
            return new ImportValidationResult(
                false, 0, 0, 0,
                [new ImportRowError(0, "File", "Sadece .xlsx ve .xls dosyaları desteklenir.")]);
        }

        return await _importService.ValidateExcelAsync(request.FileStream, cancellationToken);
    }
}
