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
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var percentage = totalRows > 0 ? (int)(processedRows * 100.0 / totalRows) : 0;
        // G133 FIX: tenant-scoped group prevents cross-tenant progress snooping.
        // Clients MUST join via JoinImportGroup (which validates tenant claim).
        var groupName = tenantId.HasValue
            ? $"import-{tenantId.Value}-{importId}"
            : $"import-{importId}";

        await _hubContext.Clients.Group(groupName)
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
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var groupName = tenantId.HasValue
            ? $"import-{tenantId.Value}-{importId}"
            : $"import-{importId}";

        await _hubContext.Clients.Group(groupName)
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
