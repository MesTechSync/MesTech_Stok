namespace MesTech.Domain.Interfaces;

/// <summary>
/// Mevcut tenant ID sağlayıcı.
/// Global Query Filter'da kullanılır.
/// </summary>
public interface ITenantProvider
{
    Guid GetCurrentTenantId();
}
