using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// Merkezi adapter metrik tanimlari.
/// Prometheus Counter ve Histogram kullanarak platform bazinda
/// API cagri sayisi, suresi, hata orani ve kargo islemlerini izler.
/// </summary>
public static class AdapterMetrics
{
    /// <summary>
    /// Toplam adapter API cagri sayisi.
    /// Label'lar: platform (trendyol, ciceksepeti, hepsiburada, opencart),
    ///            method (SyncProducts, GetOrders, UpdateStock, UpdatePrice, SendShipment, GetCategories),
    ///            status (success, error, timeout)
    /// </summary>
    public static readonly Counter ApiCallsTotal = Metrics.CreateCounter(
        "mestech_adapter_api_calls_total",
        "Total adapter API calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "platform", "method", "status" }
        });

    /// <summary>
    /// Adapter API cagri suresi (saniye).
    /// Histogram bucket'lari: 100ms, 250ms, 500ms, 1s, 2.5s, 5s, 10s
    /// </summary>
    public static readonly Histogram ApiCallDuration = Metrics.CreateHistogram(
        "mestech_adapter_api_duration_seconds",
        "Adapter API call duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "platform", "method" },
            Buckets = new[] { 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    /// <summary>
    /// Toplam kargo gonderi sayisi.
    /// Label'lar: provider (yurtici, aras, surat), status (success, error)
    /// </summary>
    public static readonly Counter CargoShipmentsTotal = Metrics.CreateCounter(
        "mestech_cargo_shipments_total",
        "Total cargo shipments created",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "status" }
        });

    /// <summary>
    /// Toplam senkronizasyon operasyonu sayisi.
    /// Label'lar: platform, direction (push, pull), status (success, error)
    /// </summary>
    public static readonly Counter SyncOperationsTotal = Metrics.CreateCounter(
        "mestech_sync_operations_total",
        "Total sync operations",
        new CounterConfiguration
        {
            LabelNames = new[] { "platform", "direction", "status" }
        });
}
