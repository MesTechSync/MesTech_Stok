using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Dalga 3 HepsiburadaListing tests — SKU mapping, commission precision, status transitions.
/// Supplements HepsiburadaListingTests.cs without duplication.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class HepsiburadaListingDalga3Tests
{
    // ── Status values ──

    [Theory]
    [InlineData("Active", true, false)]
    [InlineData("Passive", false, false)]
    [InlineData("Banned", false, true)]
    public void ListingStatus_ComputedProperties_MatchExpected(
        string status, bool expectedActive, bool expectedBanned)
    {
        var listing = FakeData.CreateHepsiburadaListing(status: status);

        listing.IsActive.Should().Be(expectedActive);
        listing.IsBanned.Should().Be(expectedBanned);
    }

    // ── Commission rate precision ──

    [Fact]
    public void CommissionRate_DecimalPrecision_TwoDecimalPlaces()
    {
        var listing = FakeData.CreateHepsiburadaListing(commission: 0.12m);

        listing.CommissionRate.Should().Be(0.12m);
    }

    [Fact]
    public void CommissionRate_HighPrecision_FourDecimalPlaces()
    {
        var listing = FakeData.CreateHepsiburadaListing(commission: 0.1575m);

        listing.CommissionRate.Should().Be(0.1575m);
    }

    [Fact]
    public void CommissionRate_ZeroIsValid()
    {
        var listing = FakeData.CreateHepsiburadaListing(commission: 0m);

        listing.CommissionRate.Should().Be(0m);
    }

    [Fact]
    public void CommissionRate_FullCommission_OneIsValid()
    {
        var listing = new HepsiburadaListing { CommissionRate = 1.0m };

        listing.CommissionRate.Should().Be(1.0m);
    }

    // ── SKU mapping ──

    [Fact]
    public void SKUMapping_BothMerchantAndHBSku_Present()
    {
        var listing = FakeData.CreateHepsiburadaListing(
            hbSku: "HB-PHONE-001",
            merchantSku: "MRC-PHONE-001");

        listing.HepsiburadaSKU.Should().Be("HB-PHONE-001");
        listing.MerchantSKU.Should().Be("MRC-PHONE-001");
    }

    [Fact]
    public void SKUMapping_DifferentFormats_BothStored()
    {
        var listing = new HepsiburadaListing
        {
            HepsiburadaSKU = "HB00000123456",
            MerchantSKU = "MYSHOP-SKU-A1B2C3",
            ListingStatus = "Active",
            CommissionRate = 0.08m
        };

        listing.HepsiburadaSKU.Should().NotBe(listing.MerchantSKU);
        listing.HepsiburadaSKU.Should().StartWith("HB");
        listing.MerchantSKU.Should().StartWith("MYSHOP");
    }

    [Fact]
    public void SKUMapping_DefaultsEmpty()
    {
        var listing = new HepsiburadaListing();

        listing.HepsiburadaSKU.Should().BeEmpty();
        listing.MerchantSKU.Should().BeEmpty();
    }

    // ── ToString ──

    [Fact]
    public void ToString_FormatsStatusAndCommission()
    {
        var listing = new HepsiburadaListing
        {
            HepsiburadaSKU = "HB-TEST",
            ListingStatus = "Active",
            CommissionRate = 0.12m
        };

        var result = listing.ToString();
        result.Should().Contain("[HB-HB-TEST]");
        result.Should().Contain("Active");
    }

    [Fact]
    public void ToString_BannedStatus_ShowsBanned()
    {
        var listing = FakeData.CreateHepsiburadaListing(
            hbSku: "HB-BAN-001",
            status: "Banned",
            commission: 0.05m);

        listing.ToString().Should().Contain("Banned");
        listing.ToString().Should().Contain("HB-BAN-001");
    }

    // ── FakeData helper ──

    [Fact]
    public void FakeData_CreateHepsiburadaListing_SetsAllFields()
    {
        var listing = FakeData.CreateHepsiburadaListing(
            hbSku: "HB-FAKE-001",
            merchantSku: "MRC-FAKE-001",
            status: "Active",
            commission: 0.15m);

        listing.HepsiburadaSKU.Should().Be("HB-FAKE-001");
        listing.MerchantSKU.Should().Be("MRC-FAKE-001");
        listing.ListingStatus.Should().Be("Active");
        listing.CommissionRate.Should().Be(0.15m);
        listing.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FakeData_CreateHepsiburadaListing_DefaultParams_GeneratesRandomSKUs()
    {
        var listing = FakeData.CreateHepsiburadaListing();

        listing.HepsiburadaSKU.Should().StartWith("HB-");
        listing.MerchantSKU.Should().StartWith("MRC-");
        listing.ListingStatus.Should().Be("Passive");
        listing.CommissionRate.Should().Be(0.12m);
    }

    // ── BaseEntity inheritance ──

    [Fact]
    public void HepsiburadaListing_SoftDelete_DefaultsFalse()
    {
        var listing = FakeData.CreateHepsiburadaListing();

        listing.IsDeleted.Should().BeFalse();
        listing.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void HepsiburadaListing_AuditFields_AutoPopulated()
    {
        var listing = FakeData.CreateHepsiburadaListing();

        listing.Id.Should().NotBeEmpty();
        listing.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        listing.CreatedBy.Should().Be("system");
    }

    // ── Status transitions ──

    [Fact]
    public void ListingStatus_TransitionActiveToPassive()
    {
        var listing = FakeData.CreateHepsiburadaListing(status: "Active");
        listing.IsActive.Should().BeTrue();

        listing.ListingStatus = "Passive";

        listing.IsActive.Should().BeFalse();
        listing.IsBanned.Should().BeFalse();
    }

    [Fact]
    public void ListingStatus_TransitionActiveToBanned()
    {
        var listing = FakeData.CreateHepsiburadaListing(status: "Active");

        listing.ListingStatus = "Banned";

        listing.IsBanned.Should().BeTrue();
        listing.IsActive.Should().BeFalse();
    }
}
