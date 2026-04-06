using System.Diagnostics.Metrics;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// MESA OS entegrasyon metrikleri.
/// .NET Meter API ile tanimlenir, prometheus-net veya OpenTelemetry ile expose edilir.
/// </summary>
public static class MesaMetrics
{
    private static readonly Meter Meter = new("MesTech.Mesa", "1.0");

    // AI cagrilari
    public static readonly Counter<long> AiRequestTotal =
        Meter.CreateCounter<long>("mesa_ai_request_total",
            description: "Total AI requests to MESA OS");

    public static readonly Histogram<double> AiRequestDuration =
        Meter.CreateHistogram<double>("mesa_ai_request_duration_seconds",
            description: "AI request latency in seconds");

    // Bot cagrilari
    public static readonly Counter<long> BotSendTotal =
        Meter.CreateCounter<long>("mesa_bot_send_total",
            description: "Total bot notification sends");

    // Consumer metrikleri
    public static readonly Counter<long> ConsumerProcessedTotal =
        Meter.CreateCounter<long>("mesa_consumer_processed_total",
            description: "Total consumer messages processed");

    // Circuit breaker state (0=Closed, 1=HalfOpen, 2=Open)
    public static readonly Gauge<int> CircuitBreakerState =
        Meter.CreateGauge<int>("mesa_circuit_breaker_state",
            description: "Circuit breaker state per service");

    // DLQ metrikleri
    public static readonly Gauge<int> DlqDepth =
        Meter.CreateGauge<int>("mesa_dlq_depth",
            description: "Current DLQ depth per queue");

    /// <summary>
    /// Record circuit breaker state change. Call from Polly onBreak/onReset/onHalfOpen.
    /// Values: 0=Closed, 1=HalfOpen, 2=Open.
    /// </summary>
    public static void RecordCircuitState(string serviceName, int state)
        => CircuitBreakerState.Record(state, new KeyValuePair<string, object?>("service", serviceName));
}
