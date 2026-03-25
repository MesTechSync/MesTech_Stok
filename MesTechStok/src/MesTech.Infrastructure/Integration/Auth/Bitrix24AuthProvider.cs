using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// Bitrix24 OAuth 2.0 Authorization Code flow.
/// Runtime uses refresh_token only — initial auth code exchange is a one-time setup.
/// Token endpoint: https://oauth.bitrix.info/oauth/token/
/// access_token TTL: 30 min, refresh buffer: 5 min.
/// CRITICAL: Each refresh returns a NEW refresh_token — old one becomes invalid.
/// </summary>
public sealed class Bitrix24AuthProvider : IAuthenticationProvider
{
    public string PlatformCode => "Bitrix24";

    private readonly HttpClient _httpClient;
    private readonly ITokenCacheProvider _tokenCache;
    private readonly ILogger<Bitrix24AuthProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(5);
    private const string DefaultTokenEndpoint = "https://oauth.bitrix.info/oauth/token/";

    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _portalDomain = string.Empty;
    private string _refreshToken = string.Empty;
    private string _tokenEndpoint = DefaultTokenEndpoint;
    private bool _isConfigured;

    public Bitrix24AuthProvider(
        HttpClient httpClient,
        ITokenCacheProvider tokenCache,
        ILogger<Bitrix24AuthProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Configure from credentials dictionary (called by Bitrix24Adapter.ConfigureAuth).
    /// </summary>
    public void Configure(string clientId, string clientSecret, string portalDomain, string refreshToken, string? tokenEndpoint = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _portalDomain = portalDomain;
        _refreshToken = refreshToken;
        _tokenEndpoint = tokenEndpoint ?? DefaultTokenEndpoint;
        _isConfigured = true;

        _logger.LogInformation("Bitrix24AuthProvider configured for portal {Portal}", _portalDomain);
    }

    public async Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        var cacheKey = $"bitrix24:{_portalDomain}";
        var cached = await _tokenCache.GetAsync(cacheKey, ct).ConfigureAwait(false);

        if (cached is not null && !IsTokenExpired(cached))
            return cached;

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            cached = await _tokenCache.GetAsync(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null && !IsTokenExpired(cached))
                return cached;

            _logger.LogInformation("Bitrix24 token refresh for portal {Portal}", _portalDomain);

            var token = await RequestRefreshTokenAsync(ct).ConfigureAwait(false);
            await _tokenCache.SetAsync(cacheKey, token, ct).ConfigureAwait(false);
            return token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        EnsureConfigured();

        var cacheKey = $"bitrix24:{_portalDomain}";

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("Bitrix24 explicit token refresh for portal {Portal}", _portalDomain);

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["refresh_token"] = refreshToken
            };

            var token = await PostTokenRequestAsync(parameters, ct).ConfigureAwait(false);
            await _tokenCache.SetAsync(cacheKey, token, ct).ConfigureAwait(false);
            return token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public bool IsTokenExpired(AuthToken token)
        => DateTime.UtcNow >= token.ExpiresAt.Subtract(RefreshBuffer);

    /// <summary>
    /// Exchange authorization code for initial tokens (one-time setup).
    /// Called during first-time configuration, not runtime.
    /// </summary>
    public async Task<AuthToken> ExchangeAuthCodeAsync(string authCode, string redirectUri, CancellationToken ct = default)
    {
        EnsureConfigured();

        var cacheKey = $"bitrix24:{_portalDomain}";

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = authCode,
            ["redirect_uri"] = redirectUri
        };

        var token = await PostTokenRequestAsync(parameters, ct).ConfigureAwait(false);
        await _tokenCache.SetAsync(cacheKey, token, ct).ConfigureAwait(false);

        // Store the new refresh token for future use
        if (!string.IsNullOrEmpty(token.RefreshToken))
            _refreshToken = token.RefreshToken;

        _logger.LogInformation("Bitrix24 initial auth code exchanged for portal {Portal}", _portalDomain);
        return token;
    }

    private async Task<AuthToken> RequestRefreshTokenAsync(CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken
        };

        var token = await PostTokenRequestAsync(parameters, ct).ConfigureAwait(false);

        // CRITICAL: Bitrix24 returns a NEW refresh_token on each refresh.
        // The old refresh_token becomes invalid immediately.
        if (!string.IsNullOrEmpty(token.RefreshToken))
        {
            _refreshToken = token.RefreshToken;
            _logger.LogDebug("Bitrix24 refresh_token rotated for portal {Portal}", _portalDomain);
        }

        return token;
    }

    private async Task<AuthToken> PostTokenRequestAsync(
        Dictionary<string, string> parameters, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(parameters);
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError(
                "Bitrix24 token request failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Bitrix24 token request failed: {response.StatusCode} — {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token missing from Bitrix24 token response");

        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 1800;
        var refreshTokenValue = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        // Bitrix24-specific: member_id and domain in token response
        var memberId = root.TryGetProperty("member_id", out var mi) ? mi.GetString() : null;
        var domain = root.TryGetProperty("domain", out var dm) ? dm.GetString() : _portalDomain;

        if (!string.IsNullOrEmpty(domain) && domain != _portalDomain)
        {
            _logger.LogInformation(
                "Bitrix24 portal domain updated: {Old} → {New}", _portalDomain, domain);
            _portalDomain = domain;
        }

        _logger.LogDebug(
            "Bitrix24 token acquired: expires_in={ExpiresIn}s, member_id={MemberId}, domain={Domain}",
            expiresIn, memberId, domain);

        return new AuthToken(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresAt: DateTime.UtcNow.AddSeconds(expiresIn),
            TokenType: "Bearer");
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "Bitrix24AuthProvider not configured. Call Configure() first.");
    }
}
