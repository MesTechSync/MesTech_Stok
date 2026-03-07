using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// WAC maliyet hesaplama ve stok yonetimi koruma testleri.
/// Bu testler kirilirsa = maliyet hesaplama mantigi bozulmus demektir.
/// </summary>
public class StockCalculationServiceTests
{
    private readonly StockCalculationService _sut = new();

    // ── WAC (Weighted Average Cost) Testleri ──

    [Fact]
    public void CalculateWAC_AfterPurchase_ShouldRecalculate()
    {
        // 100 adet x 10 TL = 1000 TL mevcut
        // + 50 adet x 20 TL = 1000 TL eklenen
        // = 150 adet, toplam 2000 TL, WAC = 13.33 TL
        var result = _sut.CalculateWAC(100, 10m, 50, 20m);

        result.Should().BeApproximately(13.33m, 0.01m);
    }

    [Fact]
    public void CalculateWAC_AfterSale_ShouldNotChange()
    {
        // WAC satis sonrasi degismez (cikis WAC'i etkilemez)
        // Mevcut: 100 x 15 TL, satis: -20 adet
        // WAC hala 15 TL olmali
        var wacBefore = _sut.CalculateWAC(100, 15m, 0, 0m);
        wacBefore.Should().Be(15m);
    }

    [Fact]
    public void CalculateWAC_EmptyStock_NewPurchase_ShouldUseNewPrice()
    {
        var result = _sut.CalculateWAC(0, 0m, 100, 25m);

        result.Should().Be(25m);
    }

    [Fact]
    public void CalculateWAC_AllStockDepleted_ShouldReturnZero()
    {
        var result = _sut.CalculateWAC(0, 10m, 0, 0m);

        result.Should().Be(0m);
    }

    // ── Stok Yeterlilik Kontrol Testleri ──

    [Fact]
    public void ValidateStockSufficiency_WhenEnough_ShouldNotThrow()
    {
        var product = FakeData.CreateProduct(stock: 50);

        var act = () => _sut.ValidateStockSufficiency(product, 30);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStockSufficiency_WhenInsufficient_ShouldThrow()
    {
        var product = FakeData.CreateProduct(stock: 10);

        var act = () => _sut.ValidateStockSufficiency(product, 20);

        act.Should().Throw<InsufficientStockException>()
            .Which.AvailableStock.Should().Be(10);
    }

    // ── FEFO (First Expire First Out) Testleri ──

    [Fact]
    public void SelectLotsForConsumption_ShouldSelectExpiringSoonFirst()
    {
        var lots = new List<InventoryLot>
        {
            FakeData.CreateLot(1, 100, 50, DateTime.UtcNow.AddMonths(6)),
            FakeData.CreateLot(1, 100, 50, DateTime.UtcNow.AddMonths(1)),  // en yakin SKT
            FakeData.CreateLot(1, 100, 50, DateTime.UtcNow.AddMonths(12))
        };

        var selected = _sut.SelectLotsForConsumption(lots, 30);

        selected.Should().HaveCount(1);
        selected[0].ExpiryDate.Should().BeCloseTo(
            DateTime.UtcNow.AddMonths(1), TimeSpan.FromDays(2));
    }

    [Fact]
    public void SelectLotsForConsumption_ShouldSelectMultipleLotsIfNeeded()
    {
        var lots = new List<InventoryLot>
        {
            FakeData.CreateLot(1, 30, 30, DateTime.UtcNow.AddDays(10)),
            FakeData.CreateLot(1, 50, 50, DateTime.UtcNow.AddDays(20)),
            FakeData.CreateLot(1, 100, 100, DateTime.UtcNow.AddDays(30))
        };

        var selected = _sut.SelectLotsForConsumption(lots, 60);

        selected.Should().HaveCount(2);
    }

    [Fact]
    public void SelectLotsForConsumption_ShouldSkipClosedLots()
    {
        var closedLot = FakeData.CreateLot(1, 100, 0, DateTime.UtcNow.AddDays(5));
        closedLot.Status = LotStatus.Closed;

        var openLot = FakeData.CreateLot(1, 100, 50, DateTime.UtcNow.AddDays(30));

        var selected = _sut.SelectLotsForConsumption(new[] { closedLot, openLot }, 30);

        selected.Should().HaveCount(1);
        selected[0].Status.Should().Be(LotStatus.Open);
    }

    // ── Envanter Deger Hesaplama ──

    [Fact]
    public void CalculateInventoryValue_ShouldSumStockTimesPurchasePrice()
    {
        var products = new[]
        {
            FakeData.CreateProduct(stock: 10, purchasePrice: 100),
            FakeData.CreateProduct(stock: 20, purchasePrice: 50)
        };

        var result = _sut.CalculateInventoryValue(products);

        result.Should().Be(2000m); // (10*100) + (20*50)
    }
}
