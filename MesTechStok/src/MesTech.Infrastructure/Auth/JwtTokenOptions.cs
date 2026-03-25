namespace MesTech.Infrastructure.Auth;

/// <summary>
/// JWT token configuration options bound from appsettings "Jwt" section.
/// Secret MUST be stored in user-secrets or environment variables — never hardcode.
/// </summary>
public sealed class JwtTokenOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
    public string Issuer { get; set; } = "mestech";
    public string Audience { get; set; } = "mestech-clients";
}
