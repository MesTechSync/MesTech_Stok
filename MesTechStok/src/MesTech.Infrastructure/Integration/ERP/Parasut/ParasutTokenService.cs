using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MesTech.Infrastructure.Monitoring;
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
    private static readonly JsonSerializerOptions s_deserializeOptions = new() { MaxDepth = 32 };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ParasutTokenService> _logger;
    private readonly ParasutOptions _options;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _companyId;

    private const string CacheKey = "Parasut:AccessToken";
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(50); // 1h token, 10min buffer
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public ParasutTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ParasutTokenService> logger,
        IOptions<ParasutOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
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
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            throw new InvalidOperationException("Parasut ERP is not configured. Set ERP:Parasut:ClientId and ERP:Parasut:ClientSecret in configuration.");

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

            _logger.LogInformation("[ParasutTokenService] Requesting new OAuth2 token");

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            };

            var content = new FormUrlEncodedContent(formData);
            using var response = await _httpClient.PostAsync(_options.TokenUrl, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError(
                    "[ParasutTokenService] Token request failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                ErpMetrics.AuthRefreshTotal.WithLabels("parasut", "error").Inc();
                throw new HttpRequestException(
                    $"Parasut OAuth2 token request failed: {response.StatusCode} — {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var tokenResponse = JsonSerializer.Deserialize<ParasutTokenResponse>(json, s_deserializeOptions);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                ErpMetrics.AuthRefreshTotal.WithLabels("parasut", "error").Inc();
                throw new InvalidOperationException("Parasut OAuth2 token response was empty or invalid");
            }

            // Cache with 50-minute TTL (10-minute buffer before actual 1h expiry)
            _cache.Set(CacheKey, tokenResponse.AccessToken, TokenTtl);
            ErpMetrics.AuthRefreshTotal.WithLabels("parasut", "success").Inc();

            _logger.LogInformation(
                "[ParasutTokenService] Token acquired, expires_in={ExpiresIn}s, cached for {CacheTtl}min",
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
        _logger.LogInformation("[ParasutTokenService] Cached token invalidated");
    }
}
