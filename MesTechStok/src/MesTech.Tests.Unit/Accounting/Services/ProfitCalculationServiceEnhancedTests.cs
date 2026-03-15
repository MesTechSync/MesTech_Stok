using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

/// <summary>
/// Enhanced ProfitCalculationService tests — FIFO COGS and DetailedProfitResult.
/// </summary>
[Trait("Category", "Unit")]
public class ProfitCalculationServiceEnhancedTests
{
    private readonly ProfitCalculationService _sut = new();

    // ── CalculateDetailed ───────────────────────────────────────────

    [Fact]
    public void CalculateDetailed_WithCOGS_ReturnsCorrectGrossProfit()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 10000m,
            totalCogs: 6000m,
            totalCommission: 500m,
            totalCargo: 300m,
            totalWithholding: 200m,
            otherExpenses: 100m);

        result.GrossProfit.Should().Be(4000m); // 10000 - 6000
    }

    [Fact]
    public void CalculateDetailed_GrossMargin_Percentage()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 10000m,
            totalCogs: 6000m,
            totalCommission: 0m,
            totalCargo: 0m,
            totalWithholding: 0m,
            otherExpenses: 0m);

        result.GrossMargin.Should().Be(40.00m); // 4000/10000 * 100
    }

    [Fact]
    public void CalculateDetailed_NetMargin_AfterAllDeductions()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 10000m,
            totalCogs: 6000m,
            totalCommission: 500m,
            totalCargo: 300m,
            totalWithholding: 200m,
            otherExpenses: 100m);

        // Net = 10000 - 6000 - 500 - 300 - 200 - 100 = 2900
        result.NetProfit.Should().Be(2900m);
        result.NetMargin.Should().Be(29.00m); // 2900/10000 * 100
    }

    [Fact]
    public void CalculateDetailed_ZeroRevenue_ReturnsZeroMargin()
    {
        var result = _sut.CalculateDetailed(0m, 0m, 0m, 0m, 0m, 0m);

        result.GrossMargin.Should().Be(0m);
        result.NetMargin.Should().Be(0m);
        result.GrossProfit.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
    }

    [Fact]
    public void CalculateDetailed_NegativeNetProfit_ReturnsNegativeMargin()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 1000m,
            totalCogs: 800m,
            totalCommission: 150m,
            totalCargo: 100m,
            totalWithholding: 50m,
            otherExpenses: 50m);

        // Net = 1000 - 800 - 150 - 100 - 50 - 50 = -150
        result.NetProfit.Should().Be(-150m);
        result.NetMargin.Should().Be(-15.00m);
    }

    [Fact]
    public void CalculateDetailed_AllFieldsRoundedTo2Decimals()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 333.333m,
            totalCogs: 111.111m,
            totalCommission: 22.222m,
            totalCargo: 11.111m,
            totalWithholding: 5.555m,
            otherExpenses: 3.333m);

        result.TotalRevenue.Should().Be(333.33m);
        result.TotalCogs.Should().Be(111.11m);
        result.TotalCommission.Should().Be(22.22m);
        result.TotalCargo.Should().Be(11.11m);
        result.TotalWithholding.Should().Be(5.56m);
        result.OtherExpenses.Should().Be(3.33m);
    }

    [Fact]
    public void CalculateDetailed_LargeRevenue_CorrectMargins()
    {
        var result = _sut.CalculateDetailed(
            totalRevenue: 1_000_000m,
            totalCogs: 600_000m,
            totalCommission: 50_000m,
            totalCargo: 30_000m,
            totalWithholding: 20_000m,
            otherExpenses: 10_000m);

        result.GrossProfit.Should().Be(400_000m);
        result.GrossMargin.Should().Be(40.00m);
        result.NetProfit.Should().Be(290_000m);
        result.NetMargin.Should().Be(29.00m);
    }

    // ── CalculateFifoCogs ───────────────────────────────────────────

    [Fact]
    public void CalculateFifoCogs_SingleLayer_UsesDirectCost()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 10, UnitCost: 50m)
        };

        _sut.CalculateFifoCogs(layers, 5).Should().Be(250m); // 5 * 50
    }

    [Fact]
    public void CalculateFifoCogs_MultipleLayersConsumed()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 5, UnitCost: 40m),
            new(Quantity: 5, UnitCost: 60m)
        };

        // FIFO: first 5 @ 40 = 200, next 3 @ 60 = 180, total = 380
        _sut.CalculateFifoCogs(layers, 8).Should().Be(380m);
    }

    [Fact]
    public void CalculateFifoCogs_AllLayersConsumed()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 3, UnitCost: 100m),
            new(Quantity: 2, UnitCost: 150m)
        };

        // 3 * 100 + 2 * 150 = 300 + 300 = 600
        _sut.CalculateFifoCogs(layers, 5).Should().Be(600m);
    }

    [Fact]
    public void CalculateFifoCogs_NoPurchasePrice_UsesAverageCost()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 4, UnitCost: 50m),
            new(Quantity: 6, UnitCost: 100m)
        };

        // Total cost = 200 + 600 = 800, Total qty = 10, Avg = 80
        // FIFO: 4*50 + 6*100 = 800 for 10 items = all layers consumed
        // Remaining 2 sold at avg cost (80): 800 + 2*80 = 960
        _sut.CalculateFifoCogs(layers, 12).Should().Be(960m);
    }

    [Fact]
    public void CalculateFifoCogs_EmptyLayers_ReturnsZero()
    {
        _sut.CalculateFifoCogs(new List<CostLayerInput>(), 5).Should().Be(0m);
    }

    [Fact]
    public void CalculateFifoCogs_NullLayers_ReturnsZero()
    {
        _sut.CalculateFifoCogs(null!, 5).Should().Be(0m);
    }

    [Fact]
    public void CalculateFifoCogs_ZeroQuantitySold_ReturnsZero()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 10, UnitCost: 50m)
        };

        _sut.CalculateFifoCogs(layers, 0).Should().Be(0m);
    }

    [Fact]
    public void CalculateFifoCogs_NegativeQuantitySold_ReturnsZero()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 10, UnitCost: 50m)
        };

        _sut.CalculateFifoCogs(layers, -1).Should().Be(0m);
    }

    [Fact]
    public void CalculateFifoCogs_ThreeLayers_ConsumesInOrder()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 2, UnitCost: 10m),
            new(Quantity: 3, UnitCost: 20m),
            new(Quantity: 5, UnitCost: 30m)
        };

        // 2*10 + 3*20 + 2*30 = 20 + 60 + 60 = 140
        _sut.CalculateFifoCogs(layers, 7).Should().Be(140m);
    }

    [Fact]
    public void CalculateFifoCogs_ResultRoundedTo2Decimals()
    {
        var layers = new List<CostLayerInput>
        {
            new(Quantity: 3, UnitCost: 33.333m)
        };

        // 2 * 33.333 = 66.666 → rounded to 66.67
        _sut.CalculateFifoCogs(layers, 2).Should().Be(66.67m);
    }

    // ── CalculateNetProfit ──────────────────────────────────────────

    [Fact]
    public void CalculateNetProfit_AllDeductions()
    {
        var result = _sut.CalculateNetProfit(
            totalRevenue: 10000m,
            totalCost: 5000m,
            totalCommission: 1000m,
            totalCargo: 500m,
            totalTax: 200m);

        result.Should().Be(3300m);
    }

    [Fact]
    public void CalculateNetProfit_ZeroRevenue()
    {
        var result = _sut.CalculateNetProfit(0m, 0m, 0m, 0m, 0m);
        result.Should().Be(0m);
    }

    // ── CalculateProfitMargin ───────────────────────────────────────

    [Fact]
    public void CalculateProfitMargin_Standard()
    {
        _sut.CalculateProfitMargin(10000m, 3300m).Should().Be(33.00m);
    }

    [Fact]
    public void CalculateProfitMargin_ZeroRevenue_ReturnsZero()
    {
        _sut.CalculateProfitMargin(0m, 100m).Should().Be(0m);
    }

    [Fact]
    public void CalculateProfitMargin_NegativeProfit()
    {
        _sut.CalculateProfitMargin(10000m, -500m).Should().Be(-5.00m);
    }
}
