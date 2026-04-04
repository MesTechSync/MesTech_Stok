using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Realtime;

/// <summary>
/// WPF uygulamasi icin self-hosted WebSocket server.
/// Port 3102'de ws://localhost:3102/ws/dashboard dinler.
/// HealthCheckEndpoint (3100) ve MesaStatusEndpoint (3101) ile ayni pattern.
/// </summary>
public sealed class RealtimeDashboardEndpoint : BackgroundService
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<RealtimeDashboardEndpoint> _logger;
    private readonly int _port;
    private HttpListener? _listener;

    public RealtimeDashboardEndpoint(
        WebSocketConnectionManager connectionManager,
        ILogger<RealtimeDashboardEndpoint> logger,
        int port = 3102)
    {
        _connectionManager = connectionManager;
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
            _logger.LogInformation("WebSocket Dashboard endpoint aktif: ws://localhost:{Port}/ws/dashboard", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = SafeHandleRequestAsync(context, stoppingToken);
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
            _logger.LogError(ex, "WebSocket Dashboard endpoint hatasi");
        }
        finally
        {
            try { _listener?.Stop(); } catch (ObjectDisposedException) { }
        }
    }

    private async Task SafeHandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            await HandleRequestAsync(context, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[WS Dashboard] Unhandled request error");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.Url?.AbsolutePath == "/ws/dashboard" && context.Request.IsWebSocketRequest)
        {
            await HandleWebSocketAsync(context, ct).ConfigureAwait(false);
        }
        else if (context.Request.Url?.AbsolutePath == "/ws/status")
        {
            // Basit HTTP status endpoint
            var response = context.Response;
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = "active",
                connections = _connectionManager.ConnectionCount,
                timestamp = DateTime.UtcNow
            });
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.StatusCode = 200;
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, ct).ConfigureAwait(false);
            response.Close();
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken ct)
    {
        WebSocketContext? wsContext = null;
        string? connectionId = null;

        try
        {
            wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
            connectionId = _connectionManager.AddConnection(wsContext.WebSocket);

            // Baglanti acik kaldigi surece dinle (heartbeat + close frame)
            var buffer = new byte[1024];
            while (wsContext.WebSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await wsContext.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await wsContext.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "Client closed", ct).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "WebSocket baglanti hatasi: {Id}", connectionId);
        }
        finally
        {
            if (connectionId != null)
            {
                _connectionManager.RemoveConnection(connectionId);
            }
        }
    }

    public override void Dispose()
    {
        _listener?.Close();
        base.Dispose();
    }
}
