using MediatR;

namespace MesTech.Application.Features.Crm.Commands.ExportCustomers;

/// <summary>
/// Musteri disa aktarma handler'i.
/// Stub: Gercek Excel/CSV uretimi Infrastructure katmaninda (IExcelExportService) yapilacak.
/// </summary>
public sealed class ExportCustomersHandler : IRequestHandler<ExportCustomersCommand, ExportCustomersResult>
{
    public Task<ExportCustomersResult> Handle(
        ExportCustomersCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        var result = new ExportCustomersResult
        {
            FileData = ReadOnlyMemory<byte>.Empty, // Infrastructure concern — IExcelExportService / ICsvExportService
            FileName = $"musteriler_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}",
            ExportedCount = 0
        };

        return Task.FromResult(result);
    }
}
