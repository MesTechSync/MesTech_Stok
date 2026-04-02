using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// DEV 1 Röntgen keşif — CariHesap/CariHareket, DropshippingPoolProduct, FulfillmentShipment testleri.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>

#region CariHesap

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CariHesapTests
{
    [Fact]
    public void GetBakiye_EmptyHareketler_ShouldReturnZero()
    {
        var cari = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Cari",
            Type = CariHesapType.Musteri
        };

        cari.GetBakiye().Should().Be(0m);
    }

    [Fact]
    public void GetBakiye_BorcAndAlacak_ShouldCalculateCorrectly()
    {
        var cari = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Cari",
            Type = CariHesapType.Musteri
        };

        cari.AddHareket(new CariHareket { Amount = 1000m, Direction = CariDirection.Borc, Description = "Fatura" });
        cari.AddHareket(new CariHareket { Amount = 300m, Direction = CariDirection.Alacak, Description = "Ödeme" });

        cari.GetBakiye().Should().Be(700m); // 1000 - 300 = 700 borçlu
    }

    [Fact]
    public void GetBakiye_MoreAlacak_ShouldBeNegative()
    {
        var cari = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Cari",
            Type = CariHesapType.Tedarikci
        };

        cari.AddHareket(new CariHareket { Amount = 200m, Direction = CariDirection.Borc, Description = "Fatura" });
        cari.AddHareket(new CariHareket { Amount = 500m, Direction = CariDirection.Alacak, Description = "Ödeme" });

        cari.GetBakiye().Should().Be(-300m); // alacaklı
    }

    [Fact]
    public void AddHareket_Null_ShouldThrow()
    {
        var cari = new CariHesap { TenantId = Guid.NewGuid(), Name = "Test" };
        var act = () => cari.AddHareket(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHareket_Valid_ShouldAddToCollection()
    {
        var cari = new CariHesap { TenantId = Guid.NewGuid(), Name = "Test" };
        var hareket = new CariHareket { Amount = 100m, Direction = CariDirection.Borc, Description = "Test" };

        cari.AddHareket(hareket);

        cari.Hareketler.Should().HaveCount(1);
        cari.Hareketler.Should().Contain(hareket);
    }
}

#endregion

#region DropshippingPoolProduct

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DropshippingPoolProductTests
{
    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var pp = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m);
        pp.Deactivate(); // ensure false first
        pp.Activate();
        pp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var pp = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m);
        pp.Deactivate();
        pp.IsActive.Should().BeFalse();
    }
}

#endregion

#region AccessLog

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class AccessLogTests
{
    [Fact]
    public void AccessLog_ShouldHaveDefaults()
    {
        var log = new AccessLog
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "LOGIN",
            IpAddress = "192.168.1.1"
        };

        log.Action.Should().Be("LOGIN");
        log.IpAddress.Should().Be("192.168.1.1");
    }
}

#endregion
