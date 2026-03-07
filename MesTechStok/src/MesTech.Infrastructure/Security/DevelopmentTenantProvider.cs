using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Development ortamı için ITenantProvider implementasyonu.
/// Tek tenant ile çalışır (TenantId = 1).
/// </summary>
public class DevelopmentTenantProvider : ITenantProvider
{
    public int GetCurrentTenantId() => 1;
}
