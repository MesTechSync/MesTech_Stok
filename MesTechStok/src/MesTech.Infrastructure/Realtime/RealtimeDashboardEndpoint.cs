using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Realtime;

/// <summary>
/// WPF uygulamasi icin self-hosted WebSocket server.
/// Port 5102'de ws://localhost:5102/ws/dashboard dinler.
/// HealthCheckEndpoint (5100) ve MesaStatusEndpoint (5101) ile ayni pattern.
/// </summary>
public class RealtimeDashboardEndpoint : BackgroundService
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<RealtimeDashboardEndpoint> _logger;
    private readonly int _port;
    private HttpListener? _listener;

    public RealtimeDashboardEndpoint(
        WebSocketConnectionManager connectionManager,
        ILogger<RealtimeDashboardEndpoint> logger,
        int port = 5102)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{_port}/");

        try
        {
            _listener.Start();
            _logger.LogInformation("WebSocket Dashboard endpoint aktif: ws://localhost:{Port}/ws/dashboard", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context, stoppingToken);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "WebSocket Dashboard endpoint hatasi");
        }
        finally
        {
            _listener?.Stop();
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.Url?.AbsolutePath == "/ws/dashboard" && context.Request.IsWebSocketRequest)
        {
            await HandleWebSocketAsync(context, ct);
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
            await response.OutputStream.WriteAsync(buffer, ct);
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
            wsContext = await context.AcceptWebSocketAsync(null);
            connectionId = _connectionManager.AddConnection(wsContext.WebSocket);

            // Baglanti acik kaldigi surece dinle (heartbeat + close frame)
            var buffer = new byte[1024];
            while (wsContext.WebSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await wsContext.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await wsContext.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "Client closed", ct);
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
