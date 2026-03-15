using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP.BizimHesap;

/// <summary>
/// Typed HttpClient wrapper for BizimHesap REST API.
/// Auth: API Key in "X-BizimHesap-ApiKey" header.
/// Base URL: configurable via ERP:BizimHesap:BaseUrl (default: "https://api.bizimhesap.com/v1/").
/// Includes JSON serialization helpers and retry-friendly error handling.
/// </summary>
public sealed class BizimHesapApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BizimHesapApiClient> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public BizimHesapApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BizimHesapApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = (configuration["ERP:BizimHesap:BaseUrl"] ?? "https://api.bizimhesap.com/v1/").TrimEnd('/');
        _apiKey = configuration["ERP:BizimHesap:ApiKey"] ?? string.Empty;
    }

    /// <summary>
    /// Base URL for BizimHesap API calls.
    /// </summary>
    public string BaseUrl => _baseUrl;

    /// <summary>
    /// Sends a GET request with API key authentication.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{endpoint}");
        SetApiKeyHeader(request);

        return await _httpClient.SendAsync(request, ct);
    }

    /// <summary>
    /// Sends a POST request with JSON body and API key authentication.
    /// </summary>
    public async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{endpoint}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        SetApiKeyHeader(request);

        return await _httpClient.SendAsync(request, ct);
    }

    /// <summary>
    /// Deserializes a JSON response body to the specified type.
    /// Returns default if deserialization fails.
    /// </summary>
    public async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[BizimHesapApiClient] Failed to deserialize response");
            return default;
        }
    }

    /// <summary>
    /// Reads the response body as a string for error logging.
    /// </summary>
    public static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        return await response.Content.ReadAsStringAsync(ct);
    }

    private void SetApiKeyHeader(HttpRequestMessage request)
    {
        request.Headers.Add("X-BizimHesap-ApiKey", _apiKey);
    }
}
