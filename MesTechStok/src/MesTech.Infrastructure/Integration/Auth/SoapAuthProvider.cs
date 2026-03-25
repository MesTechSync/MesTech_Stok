using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// SOAP tabanlı kimlik doğrulama — N11.
/// </summary>
public sealed class SoapAuthProvider : IAuthenticationProvider
{
    public string PlatformCode { get; }
    private readonly string _appKey;
    private readonly string _appSecret;
    private readonly ILogger<SoapAuthProvider> _logger;

    public SoapAuthProvider(string platformCode, string appKey, string appSecret, ILogger<SoapAuthProvider> logger)
    {
        PlatformCode = platformCode;
        _appKey = appKey;
        _appSecret = appSecret;
        _logger = logger;
    }

    public Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("SoapAuthProvider.GetTokenAsync for {Platform}", PlatformCode);
        var token = $"{_appKey}:{_appSecret}";
        return Task.FromResult(new AuthToken(
            AccessToken: token,
            RefreshToken: null,
            ExpiresAt: DateTime.MaxValue,
            TokenType: "SOAP"));
    }

    public Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => GetTokenAsync(ct);

    public bool IsTokenExpired(AuthToken token) => false;
}
