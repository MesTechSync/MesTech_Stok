using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// HTTP Basic Auth — Hepsiburada.
/// </summary>
public sealed class BasicAuthProvider : IAuthenticationProvider
{
    public string PlatformCode { get; }
    private readonly string _username;
    private readonly string _password;

    public BasicAuthProvider(string platformCode, string username, string password)
    {
        PlatformCode = platformCode;
        _username = username;
        _password = password;
    }

    public Task<AuthToken> GetTokenAsync(CancellationToken ct = default)
    {
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_username}:{_password}"));

        return Task.FromResult(new AuthToken(
            AccessToken: credentials,
            RefreshToken: null,
            ExpiresAt: DateTime.MaxValue,
            TokenType: "Basic"));
    }

    public Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => GetTokenAsync(ct);

    public bool IsTokenExpired(AuthToken token) => false;
}
