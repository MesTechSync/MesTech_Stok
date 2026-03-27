using System.Net;
using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA event monitoring HTTP endpoint.
/// http://localhost:3101/api/mesa/status adresinde JSON formatinda
/// event publish/consume istatistiklerini sunar.
/// HealthCheckEndpoint pattern'ini kullanir (port 3101).
/// </summary>
public sealed class MesaStatusEndpoint : BackgroundService
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaStatusEndpoint> _logger;
    private readonly int _port;
    private HttpListener? _listener;

    public MesaStatusEndpoint(
        IMesaEventMonitor monitor,
        ILogger<MesaStatusEndpoint> logger,
        int port = 3101)
    {
        _monitor = monitor;
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
            _logger.LogInformation(
                "MESA status endpoint aktif: http://localhost:{Port}/api/mesa/status", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = HandleRequestAsync(context)
                    .ContinueWith(t => _logger.LogError(t.Exception!, "[MESA Status] Unhandled request error"),
                        TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "MESA status endpoint hatasi");
        }
        finally
        {
            _listener?.Stop();
        }
    }

    private Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;

        try
        {
            if (context.Request.Url?.AbsolutePath == "/api/mesa/status")
            {
                var status = _monitor.GetStatus();
                var json = JsonSerializer.Serialize(status, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                response.StatusCode = 200;
                response.ContentType = "application/json";
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MESA status request hatasi");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _listener?.Close();
        base.Dispose();
    }
}
