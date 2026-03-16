using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Parasut OAuth2 Client Credentials token service.
/// POST {BaseUrl}/oauth/token — sandbox or production depending on ParasutOptions.UseSandbox.
/// Token cache: IMemoryCache, 50-minute TTL (token expires in 1h, 10min buffer).
/// Config keys: ERP:Parasut:ClientId, ERP:Parasut:ClientSecret, ERP:Parasut:CompanyId
/// </summary>
public sealed class ParasutTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ParasutTokenService> _logger;
    private readonly ParasutOptions _options;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _companyId;

    private const string CacheKey = "Parasut:AccessToken";
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(50); // 1h token, 10min buffer

    public ParasutTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ParasutTokenService> logger,
        IOptions<ParasutOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ParasutOptions();

        // Options class values take priority; fall back to legacy IConfiguration keys
        _clientId = !string.IsNullOrEmpty(_options.ClientId)
            ? _options.ClientId
            : configuration["ERP:Parasut:ClientId"] ?? string.Empty;
        _clientSecret = !string.IsNullOrEmpty(_options.ClientSecret)
            ? _options.ClientSecret
            : configuration["ERP:Parasut:ClientSecret"] ?? string.Empty;
        _companyId = !string.IsNullOrEmpty(_options.CompanyId)
            ? _options.CompanyId
            : configuration["ERP:Parasut:CompanyId"] ?? string.Empty;
    }

    /// <summary>
    /// Company ID for building API URLs: /v4/{company_id}/...
    /// </summary>
    public string CompanyId => _companyId;

    /// <summary>
    /// Gets a valid access token, using cache if available.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        _logger.LogInformation("[ParasutTokenService] Requesting new OAuth2 token");

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(_options.TokenUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "[ParasutTokenService] Token request failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Parasut OAuth2 token request failed: {response.StatusCode} — {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<ParasutTokenResponse>(json);

        if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Parasut OAuth2 token response was empty or invalid");
        }

        // Cache with 50-minute TTL (10-minute buffer before actual 1h expiry)
        _cache.Set(CacheKey, tokenResponse.AccessToken, TokenTtl);

        _logger.LogInformation(
            "[ParasutTokenService] Token acquired, expires_in={ExpiresIn}s, cached for {CacheTtl}min",
            tokenResponse.ExpiresIn, TokenTtl.TotalMinutes);

        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Invalidates the cached token (useful after 401 responses).
    /// </summary>
    public void InvalidateToken()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("[ParasutTokenService] Cached token invalidated");
    }
}
