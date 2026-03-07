using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Tenant entity koruma testleri.
/// </summary>
public class TenantTests
{
    [Fact]
    public void CreateTenant_ShouldSetProperties()
    {
        var tenant = FakeData.CreateTenant("Ali Ticaret");

        tenant.Name.Should().Be("Ali Ticaret");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tenant_ShouldHaveNavigationCollections()
    {
        var tenant = new Tenant
        {
            Name = "Test Tenant",
            TaxNumber = "1234567890"
        };

        tenant.Users.Should().NotBeNull().And.BeEmpty();
        tenant.Stores.Should().NotBeNull().And.BeEmpty();
        tenant.Warehouses.Should().NotBeNull().And.BeEmpty();
        tenant.Products.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Tenant_ShouldInheritBaseEntity()
    {
        var tenant = FakeData.CreateTenant();

        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        tenant.IsDeleted.Should().BeFalse();
    }
}
