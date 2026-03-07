using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Store entity koruma testleri.
/// </summary>
public class StoreTests
{
    [Fact]
    public void CreateStore_ShouldSetPlatformType()
    {
        var store = FakeData.CreateStore(1, PlatformType.Trendyol);

        store.TenantId.Should().Be(1);
        store.PlatformType.Should().Be(PlatformType.Trendyol);
        store.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Store_ShouldImplementITenantEntity()
    {
        var store = new Store { TenantId = 42 };

        store.TenantId.Should().Be(42);
    }

    [Fact]
    public void Store_ShouldHaveNavigationCollections()
    {
        var store = new Store
        {
            StoreName = "Test Store",
            TenantId = 1,
            PlatformType = PlatformType.OpenCart
        };

        store.Credentials.Should().NotBeNull().And.BeEmpty();
        store.ProductMappings.Should().NotBeNull().And.BeEmpty();
    }
}
