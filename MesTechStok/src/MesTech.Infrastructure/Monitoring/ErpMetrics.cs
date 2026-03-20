using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// İ-14 O-01: Prometheus metrics for ERP sync operations.
/// Tracks sync success/failure, duration, record throughput,
/// connection status, auth refreshes, and reconciliation diffs.
/// </summary>
public static class ErpMetrics
{
    /// <summary>
    /// Total ERP sync operations.
    /// Labels: provider (logo, netsis, nebim, parasut, bizimhesap),
    ///         sync_type (stock, price, order, invoice, account),
    ///         status (success, error)
    /// </summary>
    public static readonly Counter SyncTotal = Metrics.CreateCounter(
        "mestech_erp_sync_total",
        "Total ERP sync operations",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "sync_type", "status" }
        });

    /// <summary>
    /// ERP sync duration in seconds.
    /// Histogram buckets: 0.5s, 1s, 2.5s, 5s, 10s, 30s, 60s
    /// </summary>
    public static readonly Histogram SyncDuration = Metrics.CreateHistogram(
        "mestech_erp_sync_duration_seconds",
        "ERP sync duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "provider", "sync_type" },
            Buckets = new[] { 0.5, 1.0, 2.5, 5.0, 10.0, 30.0, 60.0 }
        });

    /// <summary>
    /// Total records processed in ERP sync.
    /// Labels: provider, sync_type, result (success, error, skipped)
    /// </summary>
    public static readonly Counter SyncRecordsTotal = Metrics.CreateCounter(
        "mestech_erp_sync_records_total",
        "Total records processed in ERP sync",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "sync_type", "result" }
        });

    /// <summary>
    /// ERP connection status gauge.
    /// 0 = disconnected, 1 = connected.
    /// Labels: provider
    /// </summary>
    public static readonly Gauge ConnectionStatus = Metrics.CreateGauge(
        "mestech_erp_connection_status",
        "ERP connection status (0=disconnected, 1=connected)",
        new GaugeConfiguration
        {
            LabelNames = new[] { "provider" }
        });

    /// <summary>
    /// ERP auth token refresh attempts.
    /// Labels: provider, status (success, error)
    /// </summary>
    public static readonly Counter AuthRefreshTotal = Metrics.CreateCounter(
        "mestech_erp_auth_refresh_total",
        "ERP auth token refresh attempts",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "status" }
        });

    /// <summary>
    /// Reconciliation differences found.
    /// Labels: provider, reconciliation_type (stock, account)
    /// </summary>
    public static readonly Counter ReconciliationDiffTotal = Metrics.CreateCounter(
        "mestech_erp_reconciliation_diff_total",
        "Reconciliation differences found",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "reconciliation_type" }
        });
}
