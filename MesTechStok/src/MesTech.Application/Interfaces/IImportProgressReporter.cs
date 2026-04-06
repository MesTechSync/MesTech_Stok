namespace MesTech.Application.Interfaces;

/// <summary>
/// Toplu import islemlerinde ilerleme bildirimi icin arayuz.
/// SignalR, WebSocket veya baska bir mekanizma ile implement edilir.
/// </summary>
public interface IImportProgressReporter
{
    /// <summary>
    /// Import ilerleme durumunu broadcast eder.
    /// tenantId verilirse tenant-scoped grup kullanilir (cross-tenant leak önleme — G133).
    /// </summary>
    Task ReportProgressAsync(
        Guid importId,
        int processedRows,
        int totalRows,
        int errorCount,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Import isleminin tamamlandigini bildirir.
    /// tenantId verilirse tenant-scoped grup kullanilir (cross-tenant leak önleme — G133).
    /// </summary>
    Task ReportCompletedAsync(
        Guid importId,
        int totalRows,
        int importedCount,
        int errorCount,
        TimeSpan duration,
        Guid? tenantId = null,
        CancellationToken ct = default);
}
