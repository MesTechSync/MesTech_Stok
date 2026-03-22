namespace MesTech.Application.Interfaces;

/// <summary>
/// Toplu import islemlerinde ilerleme bildirimi icin arayuz.
/// SignalR, WebSocket veya baska bir mekanizma ile implement edilir.
/// </summary>
public interface IImportProgressReporter
{
    /// <summary>
    /// Import ilerleme durumunu broadcast eder.
    /// </summary>
    Task ReportProgressAsync(
        Guid importId,
        int processedRows,
        int totalRows,
        int errorCount,
        CancellationToken ct = default);

    /// <summary>
    /// Import isleminin tamamlandigini bildirir.
    /// </summary>
    Task ReportCompletedAsync(
        Guid importId,
        int totalRows,
        int importedCount,
        int errorCount,
        TimeSpan duration,
        CancellationToken ct = default);
}
