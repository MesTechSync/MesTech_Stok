namespace MesTech.Application.Interfaces;

/// <summary>
/// JWT token generation and validation for Blazor SaaS authentication (Dalga 9).
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(Guid userId, Guid tenantId, string userName, string[] roles);
    (bool Valid, Guid UserId, Guid TenantId) ValidateToken(string token);
}
