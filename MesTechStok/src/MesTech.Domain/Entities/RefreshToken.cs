using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// JWT Refresh Token entity — OWASP ASVS V3.3 uyumlu.
/// Her kullanımda eski token revoke edilir, yeni token üretilir (rotation).
/// Token hash olarak saklanır — plain text ASLA DB'de tutulmaz.
/// </summary>
public sealed class RefreshToken : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(
        Guid userId, Guid tenantId, string tokenHash,
        int expiryDays, string? ipAddress, string? userAgent)
    {
        return new RefreshToken
        {
            UserId = userId,
            TenantId = tenantId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void Revoke(string reason, string? replacedByTokenHash = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
