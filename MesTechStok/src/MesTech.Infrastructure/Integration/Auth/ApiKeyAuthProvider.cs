using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// API Key tabanlı kimlik doğrulama — Trendyol, Ciceksepeti, Ozon, Pazarama, PttAVM.
/// </summary>
public sealed class ApiKeyAuthProvider : IAuthenticationProvider
{
    public string PlatformCode { get; }
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public ApiKeyAuthProvider(string platformCode, string apiKey, string apiSecret)
    {
        PlatformCode = platformCode;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
    }

    public Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        var token = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"));

        return Task.FromResult(new AuthToken(
            AccessToken: token,
            RefreshToken: null,
            ExpiresAt: DateTime.MaxValue,
            TokenType: "Basic"));
    }

    public Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => GetTokenAsync(ct);

    public bool IsTokenExpired(AuthToken token) => false;
}
