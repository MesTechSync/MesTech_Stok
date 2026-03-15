namespace MesTech.Domain.Interfaces;

/// <summary>
/// Mevcut kullanıcı bilgisini sağlar.
/// Login sistemi olmasa da altyapı hazır.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid TenantId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
}
