using MesTech.Application.Interfaces;
using MesTech.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MesTech.WebApi.Services;

/// <summary>
/// SignalR tabanli import ilerleme reporter.
/// Client'lar import-{importId} grubuna join ederek ilerleme alir.
/// </summary>
public sealed class SignalRImportProgressReporter : IImportProgressReporter
{
    private readonly IHubContext<MesTechHub> _hubContext;

    public SignalRImportProgressReporter(IHubContext<MesTechHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ReportProgressAsync(
        Guid importId,
        int processedRows,
        int totalRows,
        int errorCount,
        CancellationToken ct = default)
    {
        var percentage = totalRows > 0 ? (int)(processedRows * 100.0 / totalRows) : 0;

        await _hubContext.Clients.Group($"import-{importId}")
            .SendAsync("ImportProgress", new
            {
                ImportId = importId,
                ProcessedRows = processedRows,
                TotalRows = totalRows,
                Percentage = percentage,
                Errors = errorCount
            }, ct).ConfigureAwait(false);
    }

    public async Task ReportCompletedAsync(
        Guid importId,
        int totalRows,
        int importedCount,
        int errorCount,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"import-{importId}")
            .SendAsync("ImportCompleted", new
            {
                ImportId = importId,
                TotalRows = totalRows,
                ImportedCount = importedCount,
                Errors = errorCount,
                DurationMs = (int)duration.TotalMilliseconds
            }, ct).ConfigureAwait(false);
    }
}
