using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class HepsiburadaListingTests
{
    [Fact]
    public void HepsiburadaListing_DefaultStatus_ShouldBePassive()
    {
        var listing = new HepsiburadaListing();
        listing.ListingStatus.Should().Be("Passive");
        listing.IsActive.Should().BeFalse();
    }

    [Fact]
    public void HepsiburadaListing_Active_ShouldReturnTrue()
    {
        var listing = new HepsiburadaListing
        {
            HepsiburadaSKU = "HB-SKU-001",
            MerchantSKU = "MY-SKU-001",
            ListingStatus = "Active",
            CommissionRate = 0.12m
        };

        listing.IsActive.Should().BeTrue();
        listing.IsBanned.Should().BeFalse();
    }

    [Fact]
    public void HepsiburadaListing_Banned_ShouldReturnTrue()
    {
        var listing = new HepsiburadaListing { ListingStatus = "Banned" };
        listing.IsBanned.Should().BeTrue();
        listing.IsActive.Should().BeFalse();
    }

    [Fact]
    public void HepsiburadaListing_ShouldInheritBaseEntity()
    {
        var listing = new HepsiburadaListing();
        listing.Id.Should().NotBeEmpty();
        listing.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
