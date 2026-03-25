using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Services;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// Z15: StockCalculationService — overselling koruması + WAC hesabı.
/// ValidateStockSufficiency: stok yetersizse InsufficientStockException fırlatır.
/// CalculateWAC: Ağırlıklı Ortalama Maliyet hesaplaması.
/// CalculateInventoryValue: toplam envanter değeri.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Group", "StockCalculation")]
public class StockCalculationServiceTests
{
    private readonly StockCalculationService _svc = new();

    // ═══ ValidateStockSufficiency ═══

    [Fact]
    public void ValidateStockSufficiency_SufficientStock_DoesNotThrow()
    {
        var product = new Product { SKU = "VS-001", Stock = 50 };
        var act = () => _svc.ValidateStockSufficiency(product, 30);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStockSufficiency_ExactStock_DoesNotThrow()
    {
        var product = new Product { SKU = "VS-002", Stock = 10 };
        var act = () => _svc.ValidateStockSufficiency(product, 10);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStockSufficiency_InsufficientStock_ThrowsException()
    {
        var product = new Product { SKU = "VS-003", Stock = 5 };
        var act = () => _svc.ValidateStockSufficiency(product, 20);
        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void ValidateStockSufficiency_ZeroStock_ThrowsForAnyQuantity()
    {
        var product = new Product { SKU = "VS-004", Stock = 0 };
        var act = () => _svc.ValidateStockSufficiency(product, 1);
        act.Should().Throw<InsufficientStockException>();
    }

    // ═══ CalculateWAC ═══

    [Fact]
    public void CalculateWAC_FirstPurchase_ReturnsUnitCost()
    {
        // 0 stok + 100 adet × 50 TL = WAC 50 TL
        var wac = _svc.CalculateWAC(0, 0m, 100, 50m);
        wac.Should().Be(50m);
    }

    [Fact]
    public void CalculateWAC_SecondPurchase_WeightedAverage()
    {
        // 100 stok × 50 TL + 50 adet × 80 TL = 9000 / 150 = 60 TL
        var wac = _svc.CalculateWAC(100, 50m, 50, 80m);
        wac.Should().Be(60m);
    }

    [Fact]
    public void CalculateWAC_ZeroTotalStock_ReturnsZero()
    {
        var wac = _svc.CalculateWAC(0, 0m, 0, 100m);
        wac.Should().Be(0m);
    }

    // ═══ CalculateInventoryValue ═══

    [Fact]
    public void CalculateInventoryValue_MultipleProducts_ReturnsTotalCost()
    {
        var products = new[]
        {
            new Product { Stock = 10, PurchasePrice = 50m },
            new Product { Stock = 20, PurchasePrice = 30m },
            new Product { Stock = 5, PurchasePrice = 100m }
        };
        // 10×50 + 20×30 + 5×100 = 500 + 600 + 500 = 1600
        var total = _svc.CalculateInventoryValue(products);
        total.Should().Be(1600m);
    }

    [Fact]
    public void CalculateInventoryValue_EmptyList_ReturnsZero()
    {
        var total = _svc.CalculateInventoryValue(Array.Empty<Product>());
        total.Should().Be(0m);
    }

    [Fact]
    public void CalculateInventoryValue_ZeroStockProducts_ReturnsZero()
    {
        var products = new[]
        {
            new Product { Stock = 0, PurchasePrice = 50m }
        };
        var total = _svc.CalculateInventoryValue(products);
        total.Should().Be(0m);
    }
}
