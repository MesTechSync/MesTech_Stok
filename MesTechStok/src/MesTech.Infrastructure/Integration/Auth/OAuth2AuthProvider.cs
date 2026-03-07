using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// OAuth 2.0 kimlik doğrulama — Amazon, eBay.
/// </summary>
public class OAuth2AuthProvider : IAuthenticationProvider
{
    public string PlatformCode { get; }
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenEndpoint;
    private readonly ILogger<OAuth2AuthProvider> _logger;

    public OAuth2AuthProvider(string platformCode, string clientId, string clientSecret, string tokenEndpoint, ILogger<OAuth2AuthProvider> logger)
    {
        PlatformCode = platformCode;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
        _logger = logger;
    }

    public async Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("OAuth2AuthProvider.GetTokenAsync for {Platform} — endpoint: {Endpoint}", PlatformCode, _tokenEndpoint);
        // TODO: HttpClient ile token endpoint'e istek atılacak
        await Task.CompletedTask;
        return new AuthToken(
            AccessToken: "placeholder_token",
            RefreshToken: "placeholder_refresh",
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            TokenType: "Bearer");
    }

    public async Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        _logger.LogInformation("OAuth2AuthProvider.RefreshTokenAsync for {Platform}", PlatformCode);
        await Task.CompletedTask;
        return new AuthToken(
            AccessToken: "refreshed_token",
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            TokenType: "Bearer");
    }

    public bool IsTokenExpired(AuthToken token) => DateTime.UtcNow >= token.ExpiresAt;
}
