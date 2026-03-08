using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// WPF Desktop uygulaması için ITenantProvider implementasyonu.
/// Şu an hardcoded default tenant döndürür.
/// İleride login akışından kullanıcının tenant bilgisi alınacak.
/// </summary>
public class DesktopTenantProvider : ITenantProvider
{
    private static readonly Guid DefaultTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private Guid _currentTenantId = DefaultTenantId;

    public Guid GetCurrentTenantId() => _currentTenantId;

    /// <summary>
    /// Login sonrası kullanıcının tenant'ını set eder.
    /// </summary>
    public void SetTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }
}
