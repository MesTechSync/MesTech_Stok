using MesTech.Domain.Interfaces;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// Test icin degistirilebilir tenant provider.
/// SetTenant ile aktif tenant ID degistirilebilir.
/// </summary>
public class TestTenantProvider : ITenantProvider
{
    private Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid GetCurrentTenantId() => _tenantId;

    public void SetTenant(Guid tenantId) => _tenantId = tenantId;
}
