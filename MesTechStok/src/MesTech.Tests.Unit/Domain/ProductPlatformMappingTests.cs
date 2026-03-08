using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class ProductPlatformMappingTests
{
    [Fact]
    public void PlatformSpecificData_ShouldBeNullable()
    {
        var mapping = new ProductPlatformMapping
        {
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            PlatformType = PlatformType.Ciceksepeti
        };

        mapping.PlatformSpecificData.Should().BeNull();
    }

    [Fact]
    public void PlatformSpecificData_ShouldStoreJson()
    {
        var mapping = new ProductPlatformMapping
        {
            ProductId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            PlatformType = PlatformType.Hepsiburada,
            PlatformSpecificData = "{\"ListingStatus\":\"Active\",\"CommissionRate\":0.12}"
        };

        mapping.PlatformSpecificData.Should().Contain("ListingStatus");
    }

    [Fact]
    public void DefaultSyncStatus_ShouldBeNotSynced()
    {
        var mapping = new ProductPlatformMapping();
        mapping.SyncStatus.Should().Be(SyncStatus.NotSynced);
    }

    [Fact]
    public void DefaultIsEnabled_ShouldBeTrue()
    {
        var mapping = new ProductPlatformMapping();
        mapping.IsEnabled.Should().BeTrue();
    }
}
