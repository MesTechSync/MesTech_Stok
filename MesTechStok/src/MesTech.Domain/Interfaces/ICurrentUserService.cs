namespace MesTech.Domain.Interfaces;

/// <summary>
/// Mevcut kullanıcı bilgisini sağlar.
/// Login sistemi olmasa da altyapı hazır.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
}
