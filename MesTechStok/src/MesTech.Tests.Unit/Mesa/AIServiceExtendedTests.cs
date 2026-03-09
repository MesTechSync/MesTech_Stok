using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MockMesaAIService extended tests — all 5 methods coverage.
/// 10 tests: description (4 category variants), images, price (2 competitor scenarios),
/// stock prediction (2 history scenarios), category mapping (2 name variants).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MockAIServices")]
[Trait("Phase", "Dalga4")]
public class AIServiceExtendedTests
{
    private readonly MockMesaAIService _svc = new(new Mock<ILogger<MockMesaAIService>>().Object);

    // ════ 1. Electronics description ════

    [Fact]
    public async Task GenerateDescription_Electronics_ContainsTeknikOzellikler()
    {
        var result = await _svc.GenerateProductDescriptionAsync(
            "SKU-E-001", "Bluetooth Kulaklik", "Elektronik", null);

        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Teknik Ozellikler");
        result.Content.Should().Contain("Bluetooth Kulaklik");
        result.Content.Should().Contain("CE ve TSE belgeli");
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().ContainKey("seoTitle");
    }

    // ════ 2. Fashion description ════

    [Fact]
    public async Task GenerateDescription_Fashion_ContainsBedenTablosu()
    {
        var result = await _svc.GenerateProductDescriptionAsync(
            "SKU-F-001", "Erkek Tisort", "Giyim", null);

        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Beden Tablosu");
        result.Content.Should().Contain("Nefes alabilen premium kumas");
    }

    // ════ 3. Food description ════

    [Fact]
    public async Task GenerateDescription_Food_ContainsHelal()
    {
        var result = await _svc.GenerateProductDescriptionAsync(
            "SKU-G-001", "Organik Bal", "Gida", null);

        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Helal sertifikali");
        result.Content.Should().Contain("Dogal icerik");
    }

    // ════ 4. Null category → generic ════

    [Fact]
    public async Task GenerateDescription_NullCategory_ReturnsGenericDescription()
    {
        var result = await _svc.GenerateProductDescriptionAsync(
            "SKU-X-001", "Test Urun", null, null);

        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Yuksek kaliteli malzeme");
        result.Content.Should().Contain("Genel"); // fallback category
        result.Metadata.Should().ContainKey("metaDescription");
    }

    // ════ 5. Generate images ════

    [Fact]
    public async Task GenerateImages_ReturnsMockUrlsWithSKU()
    {
        var sourceUrls = new List<string>
        {
            "https://example.com/img1.jpg",
            "https://example.com/img2.jpg"
        };

        var result = await _svc.GenerateProductImagesAsync("SKU-IMG-001", sourceUrls);

        result.Success.Should().BeTrue();
        result.ImageUrls.Should().HaveCount(2);
        result.ImageUrls.Should().AllSatisfy(url =>
            url.Should().Contain("SKU-IMG-001"));
        result.ErrorMessage.Should().BeNull();
    }

    // ════ 6. RecommendPrice with competitors ════

    [Fact]
    public async Task RecommendPrice_WithCompetitors_Returns3PercentBelowAverage()
    {
        var competitors = new List<CompetitorPrice>
        {
            new("RakipA", 100m, null),
            new("RakipB", 120m, null),
            new("RakipC", 80m, null)
        };
        // Average = 100

        var result = await _svc.RecommendPriceAsync("SKU-P-001", 110m, competitors);

        result.Success.Should().BeTrue();
        result.RecommendedPrice.Should().Be(Math.Round(100m * 0.97m, 2)); // 97
        result.MinPrice.Should().Be(Math.Round(100m * 0.85m, 2)); // 85
        result.MaxPrice.Should().Be(Math.Round(100m * 1.05m, 2)); // 105
        result.Reasoning.Should().Contain("3 rakip analiz edildi");
    }

    // ════ 7. RecommendPrice without competitors ════

    [Fact]
    public async Task RecommendPrice_NoCompetitors_Uses3PercentBelowCurrentPrice()
    {
        var result = await _svc.RecommendPriceAsync("SKU-P-002", 200m, null);

        result.Success.Should().BeTrue();
        result.RecommendedPrice.Should().Be(Math.Round(200m * 0.97m, 2)); // 194
        result.Reasoning.Should().Contain("Rakip verisi yok");
    }

    // ════ 8. PredictStock with 7+ day history ════

    [Fact]
    public async Task PredictStock_WithHistory_Uses7DayAverage()
    {
        var history = Enumerable.Range(0, 10)
            .Select(i => new StockHistoryPoint(
                DateTime.UtcNow.AddDays(-i), 100 - i * 5, 5))
            .ToList();

        var result = await _svc.PredictStockAsync("SKU-S-001", 50, history);

        result.Success.Should().BeTrue();
        result.PredictedDemand.Should().BeGreaterThan(0);
        result.DaysUntilStockout.Should().BeGreaterThan(0);
        result.Reasoning.Should().Contain("Son 7 gun");
    }

    // ════ 9. PredictStock without enough history ════

    [Fact]
    public async Task PredictStock_InsufficientHistory_FallsBackToMonthlyEstimate()
    {
        var shortHistory = new List<StockHistoryPoint>
        {
            new(DateTime.UtcNow.AddDays(-1), 95, 3),
            new(DateTime.UtcNow, 92, 4)
        };

        var result = await _svc.PredictStockAsync("SKU-S-002", 90, shortHistory);

        result.Success.Should().BeTrue();
        result.Reasoning.Should().Contain("Gecmis veri yetersiz");
        result.ReorderSuggestion.Should().BeGreaterThan(0);
    }

    // ════ 10. SuggestCategory — phone product ════

    [Fact]
    public async Task SuggestCategory_PhoneProduct_ReturnsElektronik()
    {
        var result = await _svc.SuggestCategoryAsync(
            "iPhone 15 Pro Max Kilif", null, "Trendyol");

        result.Success.Should().BeTrue();
        result.SuggestedCategoryName.Should().Contain("Telefon");
        result.Confidence.Should().BeGreaterOrEqualTo(0.90);
        result.SuggestedCategoryId.Should().StartWith("Trendyol");
    }
}
