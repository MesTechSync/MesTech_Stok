using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Login denemesi kaydı — brute force koruması için DB persistence.
/// Her başarılı/başarısız deneme kayıt altına alınır.
/// </summary>
public class LoginAttempt : BaseEntity
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
        return new LoginAttempt
        {
            Username = username,
            IpAddress = ipAddress,
            Success = success,
            AttemptedAt = DateTimeOffset.UtcNow,
            UserAgent = userAgent,
            TenantId = tenantId ?? Guid.Empty
        };
    }
}
