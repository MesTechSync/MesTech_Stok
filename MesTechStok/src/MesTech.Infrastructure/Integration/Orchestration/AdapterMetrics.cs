using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// OpenTelemetry-native metrics for marketplace adapter operations.
/// Instruments: request count, latency histogram, error count.
/// Exported via OTel Collector → Prometheus → Grafana.
/// </summary>
public sealed class AdapterMetrics
{
    public static readonly string MeterName = "MesTech.Adapters";

    private readonly Counter<long> _requestCount;
    private readonly Counter<long> _errorCount;
    private readonly Histogram<double> _requestDuration;

    public AdapterMetrics(IMeterFactory? meterFactory = null)
    {
        var meter = meterFactory?.Create(MeterName) ?? new Meter(MeterName, "1.0.0");

        _requestCount = meter.CreateCounter<long>(
            "mestech.adapter.requests",
            unit: "{request}",
            description: "Total adapter API requests");

        _errorCount = meter.CreateCounter<long>(
            "mestech.adapter.errors",
            unit: "{error}",
            description: "Total adapter API errors");

        _requestDuration = meter.CreateHistogram<double>(
            "mestech.adapter.duration",
            unit: "ms",
            description: "Adapter API request duration in milliseconds");
    }

    /// <summary>Records a completed adapter request with timing.</summary>
    public void RecordRequest(string platform, string operation, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "platform", platform },
            { "operation", operation },
            { "status", success ? "success" : "error" }
        };

        _requestCount.Add(1, tags);
        _requestDuration.Record(durationMs, tags);

        if (!success)
            _errorCount.Add(1, tags);
    }

    /// <summary>Creates a scoped timer that records on dispose.</summary>
    public AdapterOperationTimer StartTimer(string platform, string operation)
        => new(this, platform, operation);
}

/// <summary>
/// Scoped timer for adapter operations — records metrics on Dispose.
/// Usage: using var timer = _metrics.StartTimer("Trendyol", "PullOrders");
/// </summary>
public sealed class AdapterOperationTimer : IDisposable
{
    private readonly AdapterMetrics _metrics;
    private readonly string _platform;
    private readonly string _operation;
    private readonly Stopwatch _sw;
    private bool _disposed;

    public bool Success { get; set; } = true;

    internal AdapterOperationTimer(AdapterMetrics metrics, string platform, string operation)
    {
        _metrics = metrics;
        _platform = platform;
        _operation = operation;
        _sw = Stopwatch.StartNew();
    }

    public void MarkError() => Success = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sw.Stop();
        _metrics.RecordRequest(_platform, _operation, _sw.Elapsed.TotalMilliseconds, Success);
    }
}
