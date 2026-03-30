using MediatR;

namespace MesTech.Application.Features.Stock.Commands.ExportStock;

/// <summary>
/// Stok disa aktarma handler'i.
/// Stub: Gercek Excel/CSV uretimi Infrastructure katmaninda (IExcelExportService) yapilacak.
/// </summary>
public sealed class ExportStockHandler : IRequestHandler<ExportStockCommand, ExportStockResult>
{
    public Task<ExportStockResult> Handle(
        ExportStockCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        var result = new ExportStockResult
        {
            FileData = [], // Infrastructure concern — IExcelExportService / ICsvExportService
            FileName = $"stok_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}",
            ExportedCount = 0
        };

        return Task.FromResult(result);
    }
}
