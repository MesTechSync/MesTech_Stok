using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Development ortamı için ITenantProvider implementasyonu.
/// Tek tenant ile çalışır.
/// </summary>
public class DevelopmentTenantProvider : ITenantProvider
{
    private static readonly Guid _defaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public Guid GetCurrentTenantId() => _defaultTenantId;
}
