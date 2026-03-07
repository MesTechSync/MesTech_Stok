using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Store entity koruma testleri.
/// </summary>
[Trait("Category", "Unit")]
public class StoreTests
{
    [Fact]
    public void CreateStore_ShouldSetPlatformType()
    {
        var tenantId = Guid.NewGuid();
        var store = FakeData.CreateStore(tenantId, PlatformType.Trendyol);

        store.TenantId.Should().Be(tenantId);
        store.PlatformType.Should().Be(PlatformType.Trendyol);
        store.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Store_ShouldImplementITenantEntity()
    {
        var tenantId = Guid.NewGuid();
        var store = new Store { TenantId = tenantId };

        store.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Store_ShouldHaveNavigationCollections()
    {
        var store = new Store
        {
            StoreName = "Test Store",
            TenantId = Guid.NewGuid(),
            PlatformType = PlatformType.OpenCart
        };

        store.Credentials.Should().NotBeNull().And.BeEmpty();
        store.ProductMappings.Should().NotBeNull().And.BeEmpty();
    }
}
