namespace MesTech.Application.Interfaces;

/// <summary>
/// JWT token generation and validation for Blazor SaaS authentication (Dalga 9).
/// Refresh token rotation: OWASP ASVS V3.3 uyumlu (STD003).
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(Guid userId, Guid tenantId, string userName, string[] roles);
    (bool Valid, Guid UserId, Guid TenantId) ValidateToken(string token);
    string GenerateRefreshToken();
    string HashToken(string token);
    (bool Valid, Guid UserId, Guid TenantId) ValidateTokenIgnoreExpiry(string token);
}
