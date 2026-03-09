using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

[Trait("Category", "Unit")]
[Trait("Feature", "MockAIServices")]
public class MockServiceTests
{
    // ══════════════════════════════════════════════
    //  MockBuyboxService
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BuyboxService_AnalyzeCompetitors_ShouldReturnDeterministicResult()
    {
        var svc = new MockBuyboxService(new Mock<ILogger<MockBuyboxService>>().Object);

        var result1 = await svc.AnalyzeCompetitorsAsync("SKU-TEST-001", 100m, "Trendyol");
        var result2 = await svc.AnalyzeCompetitorsAsync("SKU-TEST-001", 100m, "Trendyol");

        result1.HasBuybox.Should().Be(result2.HasBuybox);
        result1.Competitors.Count.Should().BeGreaterThanOrEqualTo(2);
        result1.LowestCompetitorName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BuyboxService_GetLostBuyboxes_ShouldReturnItems()
    {
        var svc = new MockBuyboxService(new Mock<ILogger<MockBuyboxService>>().Object);

        var result = await svc.GetLostBuyboxesAsync(Guid.NewGuid());

        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(item =>
        {
            item.CompetitorPrice.Should().BeLessThan(item.CurrentPrice);
            item.ProductName.Should().NotBeNullOrEmpty();
        });
    }

    // ══════════════════════════════════════════════
    //  MockStockPredictionService
    // ══════════════════════════════════════════════

    [Fact]
    public async Task StockPrediction_PredictDemand_ShouldCalculateCorrectly()
    {
        var svc = new MockStockPredictionService(new Mock<ILogger<MockStockPredictionService>>().Object);

        var result = await svc.PredictDemandAsync(Guid.NewGuid());

        result.PredictedDemand7d.Should().BeGreaterThan(0);
        result.PredictedDemand14d.Should().Be(result.PredictedDemand7d * 2);
        result.PredictedDemand30d.Should().BeGreaterThan(result.PredictedDemand14d);
        result.DaysUntilStockout.Should().BeGreaterThan(0);
        result.Confidence.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task StockPrediction_GetAlerts_ShouldFilterByThreshold()
    {
        var svc = new MockStockPredictionService(new Mock<ILogger<MockStockPredictionService>>().Object);

        var alerts = await svc.GetStockAlertsAsync(Guid.NewGuid(), daysThreshold: 5);

        alerts.Should().AllSatisfy(a => a.DaysUntilStockout.Should().BeLessThanOrEqualTo(5));
    }

    // ══════════════════════════════════════════════
    //  MockPriceOptimizationService
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PriceOptimization_ShouldRecommendAboveCost()
    {
        var svc = new MockPriceOptimizationService(new Mock<ILogger<MockPriceOptimizationService>>().Object);

        var result = await svc.OptimizePriceAsync(Guid.NewGuid(), 100m, 50m, "Trendyol");

        result.RecommendedPrice.Should().BeGreaterThan(result.CostPrice);
        result.MinPrice.Should().BeGreaterThan(result.CostPrice);
        result.ExpectedMargin.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PriceOptimization_LowMargin_ShouldSuggestClearance()
    {
        var svc = new MockPriceOptimizationService(new Mock<ILogger<MockPriceOptimizationService>>().Object);

        var result = await svc.OptimizePriceAsync(Guid.NewGuid(), 52m, 50m, "Trendyol");

        result.Strategy.Should().Be(PriceStrategy.Clearance);
    }

    // ══════════════════════════════════════════════
    //  MockProductSearchService
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ProductSearch_ShouldFindByName()
    {
        var svc = new MockProductSearchService(new Mock<ILogger<MockProductSearchService>>().Object);

        var result = await svc.SearchAsync("Kulaklik", Guid.NewGuid());

        result.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().AllSatisfy(item =>
            item.Name.Should().Contain("Kulaklik"));
    }

    [Fact]
    public async Task ProductSearch_NoMatch_ShouldReturnEmpty()
    {
        var svc = new MockProductSearchService(new Mock<ILogger<MockProductSearchService>>().Object);

        var result = await svc.SearchAsync("XYZ_NONEXISTENT_12345", Guid.NewGuid());

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProductSearch_DiscoverByCategory_ShouldFilterCorrectly()
    {
        var svc = new MockProductSearchService(new Mock<ILogger<MockProductSearchService>>().Object);

        var result = await svc.DiscoverByCategoryAsync("Elektronik", Guid.NewGuid());

        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(p => p.Category.Should().Contain("Elektronik"));
    }
}
