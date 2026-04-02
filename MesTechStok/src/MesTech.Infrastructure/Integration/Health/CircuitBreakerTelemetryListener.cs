using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using Polly.Telemetry;

namespace MesTech.Infrastructure.Integration.Health;

/// <summary>
/// Polly 8 TelemetryListener — tum circuit breaker olaylarini merkezi dinler.
/// Circuit breaker OPENED oldugunda: LogError + Prometheus metrik.
/// DEV3 TUR6: 27 adapter'a tek tek dokunmak yerine merkezi dinleme.
/// Kayit: services.Configure{ResiliencePipelineBuilder}(builder => builder.AddTelemetry(listener))
/// veya DI ile ResiliencePipelineTelemetry olarak.
/// </summary>
public sealed class CircuitBreakerTelemetryListener : TelemetryListener
{
    private readonly ILogger<CircuitBreakerTelemetryListener> _logger;

    public CircuitBreakerTelemetryListener(ILogger<CircuitBreakerTelemetryListener> logger)
    {
        _logger = logger;
    }

    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Event.EventName == "OnCircuitOpened")
        {
            var source = args.Source.PipelineName ?? "Unknown";
            _logger.LogError(
                "[CIRCUIT BREAKER OPENED] Pipeline={Pipeline} — platform API muhtemelen down. " +
                "HealthCheckJob sonraki 15dk icinde raporlayacak. Manuel kontrol oneriliyor.",
                source);

            try
            {
                AdapterMetrics.ApiCallsTotal
                    .WithLabels(source.ToLowerInvariant(), "circuit_breaker", "opened")
                    .Inc();
            }
            catch
            {
                // Prometheus unavailable — don't block pipeline
            }
        }
        else if (args.Event.EventName == "OnCircuitClosed")
        {
            var source = args.Source.PipelineName ?? "Unknown";
            _logger.LogInformation(
                "[CIRCUIT BREAKER CLOSED] Pipeline={Pipeline} — platform API restored.",
                source);

            try
            {
                AdapterMetrics.ApiCallsTotal
                    .WithLabels(source.ToLowerInvariant(), "circuit_breaker", "closed")
                    .Inc();
            }
            catch
            {
                // Prometheus unavailable
            }
        }
    }
}
