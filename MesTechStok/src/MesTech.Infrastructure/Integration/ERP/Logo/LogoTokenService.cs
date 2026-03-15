using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LogoTokenService> _logger;
    private readonly string _username;
    private readonly string _password;
    private readonly string _firmId;
    private readonly string _baseUrl;

    private const string CacheKey = "Logo:AccessToken";
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(45); // ~1h token, 15min buffer

    public LogoTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<LogoTokenService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        _logger.LogInformation("[LogoTokenService] Requesting new license token");

        var requestBody = new
        {
            username = _username,
            password = _password,
            firmId = _firmId
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/api/v1/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "[LogoTokenService] Token request failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Logo token request failed: {response.StatusCode} — {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<LogoTokenResponse>(responseJson);

        if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Logo token response was empty or invalid");
        }

        // Cache with 45-minute TTL (15-minute buffer before actual ~1h expiry)
        _cache.Set(CacheKey, tokenResponse.AccessToken, TokenTtl);

        _logger.LogInformation(
            "[LogoTokenService] Token acquired, expires_in={ExpiresIn}s, cached for {CacheTtl}min",
            tokenResponse.ExpiresIn, TokenTtl.TotalMinutes);

        return tokenResponse.AccessToken;
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
