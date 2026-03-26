using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// OAuth 2.0 kimlik dogrulama — Amazon, eBay.
/// client_credentials grant, token cache, auto-refresh.
/// </summary>
public sealed class OAuth2AuthProvider : IAuthenticationProvider
{
    public string PlatformCode { get; }
    private readonly HttpClient _httpClient;
    private readonly ITokenCacheProvider _tokenCache;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenEndpoint;
    private readonly string? _scope;
    private readonly ILogger<OAuth2AuthProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(5);
    private const int DefaultTokenExpirySeconds = 3600;

    public OAuth2AuthProvider(
        string platformCode,
        HttpClient httpClient,
        ITokenCacheProvider tokenCache,
        string clientId,
        string clientSecret,
        string tokenEndpoint,
        string? scope,
        ILogger<OAuth2AuthProvider> logger)
    {
        PlatformCode = platformCode;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        var cacheKey = $"oauth2:{PlatformCode}";
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

            _logger.LogInformation("OAuth2 token request for {Platform} at {Endpoint}",
                PlatformCode, _tokenEndpoint);

            var token = await RequestTokenAsync("client_credentials", ct).ConfigureAwait(false);
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
        var cacheKey = $"oauth2:{PlatformCode}";

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("OAuth2 token refresh for {Platform}", PlatformCode);

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
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

    private async Task<AuthToken> RequestTokenAsync(string grantType, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = grantType,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        if (!string.IsNullOrEmpty(_scope))
            parameters["scope"] = _scope;

        return await PostTokenRequestAsync(parameters, ct).ConfigureAwait(false);
    }

    private async Task<AuthToken> PostTokenRequestAsync(
        Dictionary<string, string> parameters, CancellationToken ct)
    {
        using var requestContent = new FormUrlEncodedContent(parameters);
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
        {
            Content = requestContent
        };

        var basicAuth = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("OAuth2 token request failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"OAuth2 token request failed: {response.StatusCode} — {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token missing from OAuth2 response");

        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : DefaultTokenExpirySeconds;
        var refreshTokenValue = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var tokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer";

        return new AuthToken(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresAt: DateTime.UtcNow.AddSeconds(expiresIn),
            TokenType: tokenType);
    }
}
