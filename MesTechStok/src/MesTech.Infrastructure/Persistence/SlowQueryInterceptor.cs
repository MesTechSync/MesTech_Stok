using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// EF Core slow query interceptor — 200ms üzeri sorguları loglar.
/// Prometheus histogram metriği üretir (mestech_db_query_duration_seconds).
/// </summary>
public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private const int SlowQueryThresholdMs = 200;
    private readonly ILogger<SlowQueryInterceptor> _logger;

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger)
    {
        _logger = logger;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration)
    {
        var ms = duration.TotalMilliseconds;

        // Prometheus histogram — tüm sorgular
        MesTech.Infrastructure.Monitoring.DatabaseMetrics.QueryDuration
            .Observe(duration.TotalSeconds);

        if (ms < SlowQueryThresholdMs)
            return;

        MesTech.Infrastructure.Monitoring.DatabaseMetrics.SlowQueriesTotal.Inc();

        _logger.LogWarning(
            "SLOW QUERY ({DurationMs:F0}ms): {CommandText}",
            ms,
            command.CommandText.Length > 500
                ? command.CommandText[..500] + "..."
                : command.CommandText);
    }
}
