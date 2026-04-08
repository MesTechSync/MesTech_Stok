using MesTech.Domain.Common;
using MesTech.Domain.Constants;

namespace MesTech.Domain.Entities;

/// <summary>
/// Login denemesi kaydı — brute force koruması için DB persistence.
/// Her başarılı/başarısız deneme kayıt altına alınır.
/// </summary>
public sealed class LoginAttempt : BaseEntity, ITenantEntity
{
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UserAgent { get; set; }
    public Guid TenantId { get; set; }

    public static LoginAttempt Create(
        string username, string ipAddress, bool success,
        string? userAgent = null, Guid? tenantId = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        return new LoginAttempt
        {
            Username = username,
            IpAddress = ipAddress,
            Success = success,
            AttemptedAt = DateTimeOffset.UtcNow,
            UserAgent = userAgent,
            TenantId = tenantId ?? DomainConstants.SystemTenantId
        };
    }
}
