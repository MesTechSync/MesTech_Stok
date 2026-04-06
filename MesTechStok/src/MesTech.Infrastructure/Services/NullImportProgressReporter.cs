using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Null-object fallback for IImportProgressReporter.
/// Used when SignalR is not available (Desktop, background jobs).
/// WebApi overrides this with SignalRImportProgressReporter via TryAddScoped.
/// </summary>
public sealed class NullImportProgressReporter : IImportProgressReporter
{
    public Task ReportProgressAsync(
        Guid importId, int processedRows, int totalRows, int errorCount,
        Guid? tenantId = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ReportCompletedAsync(
        Guid importId, int totalRows, int importedCount, int errorCount,
        TimeSpan duration, Guid? tenantId = null, CancellationToken ct = default)
        => Task.CompletedTask;
}
