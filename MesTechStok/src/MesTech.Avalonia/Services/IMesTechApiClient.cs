using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Typed HTTP client for MesTech WebApi calls from Avalonia ViewModels.
/// Base URL is read from appsettings.json → WebApi:BaseUrl (default: http://localhost:3100).
/// Uses IHttpClientFactory under the hood for connection pooling.
/// </summary>
public interface IMesTechApiClient
{
    Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest body, CancellationToken ct = default);
    Task<bool> PostAsync<TRequest>(string relativeUrl, TRequest body, CancellationToken ct = default);
    Task<bool> PutAsync<TRequest>(string relativeUrl, TRequest body, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);

    // ── Quick Actions (AppHub butonları — G107) ──
    Task<bool> TriggerPlatformSyncAsync(Guid tenantId, string platformCode, CancellationToken ct = default);
    Task<bool> CreateQuickInvoiceAsync(Guid tenantId, Guid orderId, CancellationToken ct = default);
}

/// <summary>
/// MesTechApiClient — wraps HttpClient for WebApi communication.
/// Registered as named HttpClient "MesTechApi" via IHttpClientFactory.
/// </summary>
public sealed class MesTechApiClient : IMesTechApiClient
{
    private readonly HttpClient _client;
    private readonly ILogger<MesTechApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MesTechApiClient(HttpClient client, ILogger<MesTechApiClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct = default)
    {
        var response = await _client.GetAsync(relativeUrl, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest body, CancellationToken ct = default)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body, _jsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync(relativeUrl, content, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
    }

    public async Task<bool> PostAsync<TRequest>(string relativeUrl, TRequest body, CancellationToken ct = default)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body, _jsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync(relativeUrl, content, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PutAsync<TRequest>(string relativeUrl, TRequest body, CancellationToken ct = default)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body, _jsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await _client.PutAsync(relativeUrl, content, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
            return false;
        }
    }

    // ── Quick Actions (G107) ──

    public async Task<bool> TriggerPlatformSyncAsync(Guid tenantId, string platformCode, CancellationToken ct = default)
        => await PostAsync($"/api/v1/platforms/{platformCode}/sync?tenantId={tenantId}", new { }, ct);

    public async Task<bool> CreateQuickInvoiceAsync(Guid tenantId, Guid orderId, CancellationToken ct = default)
        => await PostAsync($"/api/v1/invoices?tenantId={tenantId}", new { orderId }, ct);
}
