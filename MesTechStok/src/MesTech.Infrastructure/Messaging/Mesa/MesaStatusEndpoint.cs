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
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

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

        // FIX: HttpListener.GetContextAsync() does NOT support CancellationToken.
        // Register callback to close listener on shutdown — otherwise ExecuteAsync hangs forever.
        using var ctr = stoppingToken.Register(() => _listener.Close());

        try
        {
            _listener.Start();
            _logger.LogInformation(
                "MESA status endpoint aktif: http://localhost:{Port}/api/mesa/status", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = SafeHandleRequestAsync(context);
            }
        }
        catch (HttpListenerException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected: listener closed via CancellationToken callback — graceful shutdown
        }
        catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected: listener disposed during shutdown
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "MESA status endpoint hatasi");
        }
        finally
        {
            try { _listener?.Stop(); } catch (ObjectDisposedException) { }
        }
    }

    private async Task SafeHandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            await HandleRequestAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Status] Unhandled request error");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;

        try
        {
            if (context.Request.Url?.AbsolutePath == "/api/mesa/status")
            {
                var status = _monitor.GetStatus();
                var json = JsonSerializer.Serialize(status, s_jsonOptions);

                response.StatusCode = 200;
                response.ContentType = "application/json";
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
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
    }

    public override void Dispose()
    {
        _listener?.Close();
        base.Dispose();
    }
}
