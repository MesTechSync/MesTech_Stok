namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform kimlik doğrulama sağlayıcı kontratı.
/// </summary>
public interface IAuthenticationProvider
{
    string PlatformCode { get; }
    Task<AuthToken> GetTokenAsync(CancellationToken ct = default);
    Task<AuthToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    bool IsTokenExpired(AuthToken token);
}

public record AuthToken(
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt,
    string TokenType = "Bearer");
