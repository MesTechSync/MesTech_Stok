using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// Hangfire job execution Prometheus metrikleri.
/// GlobalJobFilters üzerinden tüm job'lara uygulanır — 43 job'a tek tek dokunmak gerekmez.
/// </summary>
public static class JobMetrics
{
    /// <summary>
    /// Job yürütme sayacı. Labels: job_type, status (success|failed|exception)
    /// </summary>
    public static readonly Counter JobExecutionsTotal = Metrics.CreateCounter(
        "mestech_hangfire_job_executions_total",
        "Total Hangfire job executions",
        new CounterConfiguration
        {
            LabelNames = new[] { "job_type", "status" }
        });

    /// <summary>
    /// Job yürütme süresi (saniye). Labels: job_type
    /// Buckets: 100ms, 500ms, 1s, 5s, 30s, 60s, 300s
    /// </summary>
    public static readonly Histogram JobDurationSeconds = Metrics.CreateHistogram(
        "mestech_hangfire_job_duration_seconds",
        "Hangfire job execution duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "job_type" },
            Buckets = new[] { 0.1, 0.5, 1.0, 5.0, 30.0, 60.0, 300.0 }
        });

    /// <summary>
    /// Şu an çalışan job sayısı (Gauge). Labels: job_type
    /// </summary>
    public static readonly Gauge JobsInFlight = Metrics.CreateGauge(
        "mestech_hangfire_jobs_in_flight",
        "Number of Hangfire jobs currently executing",
        new GaugeConfiguration
        {
            LabelNames = new[] { "job_type" }
        });

    /// <summary>
    /// Kuyrukta bekleyen job tahmini (Gauge). Labels: queue
    /// HangfireQueueMonitorJob tarafından doldurulur.
    /// </summary>
    public static readonly Gauge JobQueueLength = Metrics.CreateGauge(
        "mestech_hangfire_queue_length",
        "Estimated number of jobs enqueued",
        new GaugeConfiguration
        {
            LabelNames = new[] { "queue" }
        });
}
