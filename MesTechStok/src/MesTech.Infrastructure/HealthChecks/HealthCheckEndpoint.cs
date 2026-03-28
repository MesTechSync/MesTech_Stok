using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace MesTech.Infrastructure.HealthChecks;

/// <summary>
/// WPF uygulamasi icin lightweight HTTP health check endpoint.
/// /health adresinde JSON formatinda saglik durumu sunar.
/// Docker healthcheck ve Prometheus icin kullanilir.
/// </summary>
public sealed class HealthCheckEndpoint : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckEndpoint> _logger;
    private readonly int _port;
    private HttpListener? _listener;

    public HealthCheckEndpoint(
        HealthCheckService healthCheckService,
        ILogger<HealthCheckEndpoint> logger,
        int port = 3100)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");

        try
        {
            _listener.Start();
            _logger.LogInformation("Health + Metrics endpoint aktif: http://localhost:{Port}/health ve /metrics", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                _ = SafeHandleRequestAsync(context, stoppingToken);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Health check endpoint hatasi");
        }
        finally
        {
            _listener?.Stop();
        }
    }

    private async Task SafeHandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            await HandleRequestAsync(context, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Unhandled error in health check request handler");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url?.AbsolutePath == "/health")
            {
                var report = await _healthCheckService.CheckHealthAsync(ct);
                var result = new
                {
                    status = report.Status.ToString().ToLowerInvariant(),
                    timestamp = DateTime.UtcNow,
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString().ToLowerInvariant(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        error = e.Value.Exception?.Message
                    })
                };

                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                response.ContentType = "application/json";
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, ct);
            }
            else if (request.Url?.AbsolutePath == "/metrics")
            {
                response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                response.StatusCode = 200;
                using var metricsStream = new MemoryStream();
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(metricsStream, ct);
                var metricsBuffer = metricsStream.ToArray();
                response.ContentLength64 = metricsBuffer.Length;
                await response.OutputStream.WriteAsync(metricsBuffer, ct);
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check request hatasi");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    public override void Dispose()
    {
        _listener?.Close();
        base.Dispose();
    }
}
