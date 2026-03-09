using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// Extended mock service tests — untested methods from Buybox, StockPrediction,
/// PriceOptimization, ProductSearch services.
/// 7 tests: buybox positions, reorder suggest, bulk optimize, price history,
/// high margin strategy, find similar, unknown category mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MockAIServices")]
[Trait("Phase", "Dalga4")]
public class MockServiceExtendedTests
{
    // ════ 1. CheckBuyboxPositions — returns 5 items with alternating buybox ════

    [Fact]
    public async Task BuyboxService_CheckPositions_Returns5AlternatingBuybox()
    {
        var svc = new MockBuyboxService(new Mock<ILogger<MockBuyboxService>>().Object);

        var positions = await svc.CheckBuyboxPositionsAsync(Guid.NewGuid(), "Trendyol");

        positions.Should().HaveCount(5);
        positions[0].HasBuybox.Should().BeTrue();  // even index
        positions[1].HasBuybox.Should().BeFalse(); // odd index
        positions[2].HasBuybox.Should().BeTrue();  // even index
        positions.Should().AllSatisfy(p =>
        {
            p.SKU.Should().StartWith("SKU-MOCK-");
            p.PriceDiff.Should().BeGreaterOrEqualTo(0);
        });
    }

    // ════ 2. SuggestReorder — calculates quantity and cost ════

    [Fact]
    public async Task StockPrediction_SuggestReorder_CalculatesQuantityAndCost()
    {
        var svc = new MockStockPredictionService(new Mock<ILogger<MockStockPredictionService>>().Object);

        var suggestion = await svc.SuggestReorderAsync(Guid.NewGuid());

        suggestion.SuggestedQuantity.Should().BeGreaterThan(0);
        suggestion.SuggestedQuantity.Should().BePositive();
        suggestion.EstimatedCost.Should().BeGreaterThan(0);
        suggestion.Reasoning.Should().Contain("siparis onerilir");
        suggestion.SuggestedOrderDate.Should().BeAfter(DateTime.UtcNow);
    }

    // ════ 3. OptimizeBulk — returns 3 items with Competitive strategy ════

    [Fact]
    public async Task PriceOptimization_Bulk_Returns3Items()
    {
        var svc = new MockPriceOptimizationService(new Mock<ILogger<MockPriceOptimizationService>>().Object);

        var results = await svc.OptimizeBulkAsync(Guid.NewGuid(), "Trendyol");

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r =>
        {
            r.SKU.Should().StartWith("SKU-BULK-");
            r.RecommendedPrice.Should().BeGreaterThan(r.CostPrice);
            r.Strategy.Should().Be(PriceStrategy.Competitive);
            r.Reasoning.Should().Contain("%25 marj hedefi");
        });
    }

    // ════ 4. GetPriceHistory — returns daily price points ════

    [Fact]
    public async Task PriceOptimization_History_ReturnsPointsForDays()
    {
        var svc = new MockPriceOptimizationService(new Mock<ILogger<MockPriceOptimizationService>>().Object);

        var history = await svc.GetPriceHistoryAsync(Guid.NewGuid(), days: 14);

        history.PricePoints.Should().HaveCount(15); // 14 days + today
        history.AiRecommendations.Should().NotBeEmpty();
        history.AveragePrice.Should().BeGreaterThan(0);
        history.AverageAiPrice.Should().BeGreaterThan(0);
        history.PricePoints.Should().AllSatisfy(p =>
            p.Source.Should().Be("platform"));
        history.AiRecommendations.Should().AllSatisfy(p =>
            p.Source.Should().Be("ai.price.recommended"));
    }

    // ════ 5. High margin → MarginMaximize strategy ════

    [Fact]
    public async Task PriceOptimization_HighMargin_ReturnsMarginMaximize()
    {
        var svc = new MockPriceOptimizationService(new Mock<ILogger<MockPriceOptimizationService>>().Object);

        // margin = (300 - 100) / 300 = 66.7% > 40%
        var result = await svc.OptimizePriceAsync(Guid.NewGuid(), 300m, 100m, "Trendyol");

        result.Strategy.Should().Be(PriceStrategy.MarginMaximize);
        result.Reasoning.Should().Contain("Yuksek marj");
    }

    // ════ 6. FindSimilar — returns same category products ════

    [Fact]
    public async Task ProductSearch_FindSimilar_ReturnsSameCategory()
    {
        var svc = new MockProductSearchService(new Mock<ILogger<MockProductSearchService>>().Object);

        var similar = await svc.FindSimilarAsync(Guid.NewGuid(), maxResults: 5);

        similar.Should().NotBeEmpty();
        similar.Should().AllSatisfy(s =>
        {
            s.Similarity.Should().BeGreaterThan(0);
            s.Similarity.Should().BeLessThanOrEqualTo(1.0);
            s.Reason.Should().Be("Ayni kategori");
        });

        // Similarity decreases with each result
        for (int i = 1; i < similar.Count; i++)
        {
            similar[i].Similarity.Should().BeLessThan(similar[i - 1].Similarity);
        }
    }

    // ════ 7. SuggestCategory — unknown product → Genel ════

    [Fact]
    public async Task AIService_SuggestCategory_UnknownProduct_ReturnsGenel()
    {
        var svc = new MockMesaAIService(new Mock<ILogger<MockMesaAIService>>().Object);

        var result = await svc.SuggestCategoryAsync(
            "Bilinmeyen Ozel Urun XYZ", null, "OpenCart");

        result.Success.Should().BeTrue();
        result.SuggestedCategoryName.Should().Be("Genel Urunler");
        result.Confidence.Should().Be(0.60);
        result.SuggestedCategoryId.Should().StartWith("OpenCart");
    }
}
