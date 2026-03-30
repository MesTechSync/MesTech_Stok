using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP.Logo;

/// <summary>
/// Logo L-Object REST API license token service.
/// POST {BaseUrl}/api/v1/token { username, password, firmId }
/// Token cache: IMemoryCache, 45-minute TTL (token expires ~1h, 15min buffer).
/// Config keys: ERP:Logo:Username, ERP:Logo:Password, ERP:Logo:FirmId, ERP:Logo:BaseUrl
/// </summary>
public sealed class LogoTokenService
{
    private static readonly JsonSerializerOptions s_caseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient; // Sentry: HTTP client with timeout config
    private readonly IMemoryCache _cache;
    private readonly ILogger<LogoTokenService> _logger; // Sentry: Structured logging
    private readonly string _username;
    private readonly string _password;
    private readonly string _firmId;
    private readonly string _baseUrl;

    private const string CacheKey = "Logo:AccessToken";
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(45); // ~1h token, 15min buffer
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public LogoTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<LogoTokenService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _username = configuration["ERP:Logo:Username"] ?? string.Empty;
        _password = configuration["ERP:Logo:Password"] ?? string.Empty;
        _firmId = configuration["ERP:Logo:FirmId"] ?? string.Empty;
        _baseUrl = configuration["ERP:Logo:BaseUrl"] ?? "https://localhost/logo-rest/api/";
    }

    /// <summary>
    /// Base URL for Logo REST API calls.
    /// </summary>
    public string BaseUrl => _baseUrl.TrimEnd('/');

    /// <summary>
    /// Gets a valid access token, using cache if available.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: return cached token without lock
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        // Double-check locking: only one thread fetches a new token
        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Re-check after acquiring lock — another thread may have already fetched
            if (_cache.TryGetValue(CacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            _logger.LogInformation("[LogoTokenService] Requesting new license token"); // Sentry: Token request tracking

            var requestBody = new
            {
                username = _username,
                password = _password,
                firmId = _firmId
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/api/v1/token", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError(
                    "[LogoTokenService] Token request failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                ErpMetrics.AuthRefreshTotal.WithLabels("logo", "error").Inc();
                throw new HttpRequestException(
                    $"Logo token request failed: {response.StatusCode} — {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var tokenResponse = JsonSerializer.Deserialize<LogoTokenResponse>(responseJson, s_caseInsensitiveJson);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErpMetrics.AuthRefreshTotal.WithLabels("logo", "error").Inc();
                throw new InvalidOperationException("Logo token response was empty or invalid");
            }

            // Cache with 45-minute TTL (15-minute buffer before actual ~1h expiry)
            _cache.Set(CacheKey, tokenResponse.AccessToken, TokenTtl);
            ErpMetrics.AuthRefreshTotal.WithLabels("logo", "success").Inc();

            _logger.LogInformation(
                "[LogoTokenService] Token acquired, expires_in={ExpiresIn}s, cached for {CacheTtl}min",
                tokenResponse.ExpiresIn, TokenTtl.TotalMinutes);

            return tokenResponse.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Invalidates the cached token (useful after 401 responses).
    /// </summary>
    public void InvalidateToken()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("[LogoTokenService] Cached token invalidated");
    }
}
