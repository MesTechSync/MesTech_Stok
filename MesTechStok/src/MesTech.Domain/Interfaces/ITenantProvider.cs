namespace MesTech.Domain.Interfaces;

/// <summary>
/// Mevcut tenant ve kullanıcı bilgisi sağlayıcı.
/// Global Query Filter + Audit Trail'de kullanılır.
/// </summary>
public interface ITenantProvider
{
    Guid GetCurrentTenantId();

    /// <summary>
    /// Mevcut kullanıcı adı — audit trail (CreatedBy/UpdatedBy) için.
    /// Kullanıcı oturumu yoksa "system" döner.
    /// </summary>
    string GetCurrentUserName() => "system";
}
