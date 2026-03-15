using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Domain.Services;

namespace MesTech.Tests.Unit.Domain.Services;

/// <summary>
/// Domain-level AutoShipmentService testleri.
/// Recommend() kural motoru: platform tercihi, COD, agir paket, buyuksehir/tasra.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AutoShipment")]
[Trait("Phase", "Dalga12")]
public class AutoShipmentDomainServiceTests
{
    private readonly AutoShipmentService _sut = new();

    // ══════════════════════════════════════════════════════════════════════════
    // Platform Preference Rules
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Recommend — Trendyol platform preference maps to YurticiKargo")]
    public void Recommend_TrendyolPlatform_ReturnsYurticiKargo()
    {
        var request = CreateRequest(city: "Istanbul", platform: PlatformType.Trendyol);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.YurticiKargo);
        result.Reason.Should().Contain("Trendyol");
    }

    [Fact(DisplayName = "Recommend — Hepsiburada metropolitan city maps to Hepsijet")]
    public void Recommend_HepsiburadaMetropolitanCity_ReturnsHepsijet()
    {
        var request = CreateRequest(city: "Istanbul", platform: PlatformType.Hepsiburada);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.Hepsijet);
        result.Reason.Should().Contain("Hepsiburada");
    }

    [Fact(DisplayName = "Recommend — Hepsiburada rural city falls back to YurticiKargo")]
    public void Recommend_HepsiburadaRuralCity_FallsBackToYurticiKargo()
    {
        var request = CreateRequest(city: "Agri", platform: PlatformType.Hepsiburada);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.YurticiKargo,
            "Hepsijet not available in non-metropolitan cities");
        result.Reason.Should().Contain("buyuksehir degil");
    }

    [Fact(DisplayName = "Recommend — N11 platform preference maps to ArasKargo")]
    public void Recommend_N11Platform_ReturnsArasKargo()
    {
        var request = CreateRequest(city: "Ankara", platform: PlatformType.N11);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.ArasKargo);
    }

    [Fact(DisplayName = "Recommend — Amazon platform preference maps to UPS")]
    public void Recommend_AmazonPlatform_ReturnsUPS()
    {
        var request = CreateRequest(city: "Izmir", platform: PlatformType.Amazon);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.UPS);
    }

    [Fact(DisplayName = "Recommend — Ciceksepeti platform preference maps to MngKargo")]
    public void Recommend_CiceksepetiPlatform_ReturnsMngKargo()
    {
        var request = CreateRequest(city: "Bursa", platform: PlatformType.Ciceksepeti);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.MngKargo);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Cash On Delivery
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Recommend — COD order maps to YurticiKargo regardless of city")]
    public void Recommend_CashOnDelivery_ReturnsYurticiKargo()
    {
        var request = CreateRequest(city: "Trabzon", isCod: true);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.YurticiKargo);
        result.Reason.Should().Contain("Kapida odeme");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Heavy/Large Package
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Recommend — heavy package (>=30kg) maps to ArasKargo")]
    public void Recommend_HeavyPackage_ReturnsArasKargo()
    {
        var request = CreateRequest(city: "Konya", weightKg: 35m);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.ArasKargo);
        result.Reason.Should().Contain("Agir");
    }

    [Fact(DisplayName = "Recommend — large desi (>=50) maps to ArasKargo")]
    public void Recommend_LargeDesi_ReturnsArasKargo()
    {
        var request = CreateRequest(city: "Antalya", desi: 55m);

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.ArasKargo);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Metropolitan vs Rural
    // ══════════════════════════════════════════════════════════════════════════

    [Theory(DisplayName = "Recommend — metropolitan cities get SuratKargo")]
    [InlineData("Istanbul")]
    [InlineData("Ankara")]
    [InlineData("Izmir")]
    [InlineData("Bursa")]
    [InlineData("Antalya")]
    public void Recommend_MetropolitanCity_ReturnsSuratKargo(string city)
    {
        var request = CreateRequest(city: city); // no platform, no COD, normal weight

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.SuratKargo);
        result.Reason.Should().Contain("buyuksehir");
    }

    [Theory(DisplayName = "Recommend — rural cities get PttKargo")]
    [InlineData("Agri")]
    [InlineData("Mus")]
    [InlineData("Artvin")]
    [InlineData("Sinop")]
    public void Recommend_RuralCity_ReturnsPttKargo(string city)
    {
        var request = CreateRequest(city: city); // no platform, no COD, normal weight

        var result = _sut.Recommend(request);

        result.Provider.Should().Be(CargoProvider.PttKargo);
        result.Reason.Should().Contain("tasra");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Recommend — null request throws ArgumentNullException")]
    public void Recommend_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Recommend(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recommend — empty destination city throws ArgumentException")]
    public void Recommend_EmptyDestinationCity_ThrowsArgumentException()
    {
        var act = () => _sut.Recommend(new ShipmentRequest("", 2m, 5m, false));
        act.Should().Throw<ArgumentException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper
    // ══════════════════════════════════════════════════════════════════════════

    private static ShipmentRequest CreateRequest(
        string city = "Istanbul",
        decimal weightKg = 2m,
        decimal desi = 5m,
        bool isCod = false,
        PlatformType? platform = null,
        decimal? orderAmount = null)
    {
        return new ShipmentRequest(
            DestinationCity: city,
            WeightKg: weightKg,
            Desi: desi,
            IsCashOnDelivery: isCod,
            SourcePlatform: platform,
            OrderAmount: orderAmount);
    }
}
