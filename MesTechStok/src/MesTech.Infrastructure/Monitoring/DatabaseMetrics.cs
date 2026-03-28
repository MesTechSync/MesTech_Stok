using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// EF Core veritabani performans metrikleri.
/// SlowQueryInterceptor tarafindan beslenir.
/// </summary>
public static class DatabaseMetrics
{
    /// <summary>
    /// Sorgu suresi histogram — tum EF Core sorgulari.
    /// Bucket'lar: 10ms, 50ms, 100ms, 200ms, 500ms, 1s, 2s, 5s
    /// Grafana: histogram_quantile(0.95, mestech_db_query_duration_seconds)
    /// </summary>
    public static readonly Histogram QueryDuration = Metrics.CreateHistogram(
        "mestech_db_query_duration_seconds",
        "EF Core query execution duration in seconds",
        new HistogramConfiguration
        {
            Buckets = [0.01, 0.05, 0.1, 0.2, 0.5, 1.0, 2.0, 5.0]
        });

    /// <summary>
    /// Yavas sorgu sayaci (200ms+).
    /// Grafana alert: rate(mestech_db_slow_queries_total[5m]) > 10
    /// </summary>
    public static readonly Counter SlowQueriesTotal = Metrics.CreateCounter(
        "mestech_db_slow_queries_total",
        "Total number of slow database queries (>200ms)");
}
