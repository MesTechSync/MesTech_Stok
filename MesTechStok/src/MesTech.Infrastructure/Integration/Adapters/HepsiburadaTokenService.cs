using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Hepsiburada OAuth token service — K1c-02 P0 duzeltme.
/// POST /auth/token with username+password, cache token for 55 minutes (5-min safety margin).
/// HB tokens expire in 60 minutes; we refresh at 55 to avoid mid-request expiry.
/// Config keys: Hepsiburada:Username, Hepsiburada:Password, Hepsiburada:AuthUrl
/// </summary>
public sealed class HepsiburadaTokenService
{
    private static readonly JsonSerializerOptions s_caseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HepsiburadaTokenService> _logger;
    private readonly string _username;
    private readonly string _password;
    private readonly string _authUrl;

    private const string CacheKey = "Hepsiburada:OAuthToken";
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(55);
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    public HepsiburadaTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<HepsiburadaTokenService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _username = configuration["Hepsiburada:Username"] ?? string.Empty;
        _password = configuration["Hepsiburada:Password"] ?? string.Empty;
        _authUrl = configuration["Hepsiburada:AuthUrl"] ?? "https://auth.hepsiburada.com/oauth/token";

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                DelayGenerator = args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .HandleResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "[HepsiburadaTokenService] Token retry {Attempt} after {Delay}ms (status: {Status})",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
                    return default;
                }
            })
            .Build();
    }

    /// <summary>
    /// Gets a valid OAuth token, using cache if available.
    /// Thread-safe: concurrent callers share one token request via SemaphoreSlim.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock — another thread may have refreshed
            if (_cache.TryGetValue(CacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            _logger.LogInformation("[HepsiburadaTokenService] Requesting new OAuth token");
            var sw = Stopwatch.StartNew();

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _username,
                ["password"] = _password
            };

            var response = await _retryPipeline.ExecuteAsync(async token =>
            {
                var reqContent = new FormUrlEncodedContent(formData);
                return await _httpClient.PostAsync(_authUrl, reqContent, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError(
                    "[HepsiburadaTokenService] Token request failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"Hepsiburada OAuth token request failed: {response.StatusCode} — {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var tokenResponse = JsonSerializer.Deserialize<HbTokenResponse>(json, s_caseInsensitiveJson);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Hepsiburada OAuth token response was empty or invalid");
            }

            _cache.Set(CacheKey, tokenResponse.AccessToken, TokenTtl);

            sw.Stop();
            _logger.LogInformation(
                "[HepsiburadaTokenService] Token acquired in {ElapsedMs}ms, expires_in={ExpiresIn}s, cached for {CacheTtl}min",
                sw.ElapsedMilliseconds, tokenResponse.ExpiresIn, TokenTtl.TotalMinutes);

            return tokenResponse.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Invalidates the cached token — called on 401 responses to force re-auth.
    /// </summary>
    public void InvalidateToken()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("[HepsiburadaTokenService] Cached token invalidated");
    }
}

/// <summary>
/// Hepsiburada OAuth token response model.
/// </summary>
internal sealed class HbTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
