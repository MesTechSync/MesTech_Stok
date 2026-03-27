using System.Text.Json;

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
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

/// <summary>
/// MesTechApiClient — wraps HttpClient for WebApi communication.
/// Registered as named HttpClient "MesTechApi" via IHttpClientFactory.
/// </summary>
public sealed class MesTechApiClient : IMesTechApiClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MesTechApiClient(HttpClient client)
    {
        _client = client;
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

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
