using MesTech.Domain.Interfaces;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// Test icin degistirilebilir tenant provider.
/// SetTenant ile aktif tenant ID degistirilebilir.
/// </summary>
public class TestTenantProvider : ITenantProvider
{
    private int _tenantId = 1;

    public int GetCurrentTenantId() => _tenantId;

    public void SetTenant(int tenantId) => _tenantId = tenantId;
}
