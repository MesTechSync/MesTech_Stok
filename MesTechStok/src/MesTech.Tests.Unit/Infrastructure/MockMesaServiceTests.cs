using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// MockMesaAIService + MockMesaBotService unit tests — DEV5 cross-check for DEV6's mock implementations.
/// Verifies every interface method returns Success and expected mock data.
/// </summary>
[Trait("Category", "Unit")]
public class MockMesaServiceTests
{
    // ══════════════════════════════════════════════
    //  MockMesaAIService Tests
    // ══════════════════════════════════════════════

    private static MockMesaAIService CreateAIService()
    {
        var logger = new Mock<ILogger<MockMesaAIService>>();
        return new MockMesaAIService(logger.Object);
    }

    [Fact]
    public async Task AI_GenerateProductDescription_ShouldReturnSuccessWithContent()
    {
        var service = CreateAIService();

        var result = await service.GenerateProductDescriptionAsync(
            "SKU-001", "Test Urun", "Elektronik",
            new List<string> { "https://img.test/1.jpg" });

        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrWhiteSpace();
        result.Content.Should().Contain("Test Urun");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task AI_GenerateProductDescription_ShouldIncludeSeoMetadata()
    {
        var service = CreateAIService();

        var result = await service.GenerateProductDescriptionAsync(
            "SKU-002", "Kablosuz Kulaklik", "Elektronik", null);

        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().ContainKey("seoTitle");
        result.Metadata.Should().ContainKey("metaDescription");
        result.Metadata.Should().ContainKey("bulletPoint1");
    }

    [Fact]
    public async Task AI_GenerateProductImages_ShouldReturnMockUrls()
    {
        var service = CreateAIService();
        var sources = new List<string> { "img1.jpg", "img2.jpg", "img3.jpg" };

        var result = await service.GenerateProductImagesAsync("SKU-003", sources);

        result.Success.Should().BeTrue();
        result.ImageUrls.Should().HaveCount(3);
        result.ImageUrls.Should().AllSatisfy(url => url.Should().Contain("SKU-003"));
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task AI_RecommendPrice_ShouldReturnLowerPriceThanCurrent()
    {
        var service = CreateAIService();

        var result = await service.RecommendPriceAsync("SKU-004", 100m, null);

        result.Success.Should().BeTrue();
        result.RecommendedPrice.Should().BeLessThan(100m); // Mock returns 95%
        result.MinPrice.Should().BeLessThan(result.RecommendedPrice);
        result.MaxPrice.Should().BeGreaterThan(result.RecommendedPrice);
        result.Reasoning.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AI_PredictStock_ShouldReturnPredictionWithDaysUntilStockout()
    {
        var service = CreateAIService();

        var result = await service.PredictStockAsync("SKU-005", 300, null);

        result.Success.Should().BeTrue();
        result.PredictedDemand.Should().BeGreaterThan(0);
        result.ReorderSuggestion.Should().BeGreaterThan(0);
        result.DaysUntilStockout.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AI_SuggestCategory_ShouldReturnMappingWithConfidence()
    {
        var service = CreateAIService();

        var result = await service.SuggestCategoryAsync(
            "Samsung Galaxy S24", "Akilli telefon", "Trendyol");

        result.Success.Should().BeTrue();
        result.SuggestedCategoryId.Should().NotBeNullOrWhiteSpace();
        result.SuggestedCategoryName.Should().NotBeNullOrWhiteSpace();
        result.Confidence.Should().BeGreaterThan(0).And.BeLessOrEqualTo(1.0);
    }

    // ══════════════════════════════════════════════
    //  MockMesaBotService Tests
    // ══════════════════════════════════════════════

    private static MockMesaBotService CreateBotService()
    {
        var logger = new Mock<ILogger<MockMesaBotService>>();
        return new MockMesaBotService(logger.Object);
    }

    [Fact]
    public async Task Bot_SendWhatsApp_ShouldReturnTrue()
    {
        var service = CreateBotService();

        var result = await service.SendWhatsAppNotificationAsync(
            "+905551234567", "order_confirmed",
            new Dictionary<string, string>
            {
                ["orderNumber"] = "ORD-12345",
                ["customerName"] = "Test Musteri"
            });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Bot_SendTelegramAlert_ShouldReturnTrue_ForAllLevels()
    {
        var service = CreateBotService();

        var info = await service.SendTelegramAlertAsync(
            "stock-alerts", "Stok seviyesi normal", TelegramAlertLevel.Info);
        var warning = await service.SendTelegramAlertAsync(
            "stock-alerts", "Stok dusuk: SKU-001", TelegramAlertLevel.Warning);
        var critical = await service.SendTelegramAlertAsync(
            "stock-alerts", "Stok tukendi: SKU-001", TelegramAlertLevel.Critical);

        info.Should().BeTrue();
        warning.Should().BeTrue();
        critical.Should().BeTrue();
    }

    [Fact]
    public async Task Bot_SendBulkNotification_ShouldReturnTrue()
    {
        var service = CreateBotService();

        var result = await service.SendBulkNotificationAsync(
            NotificationChannel.WhatsApp,
            new List<string> { "+905551111111", "+905552222222", "+905553333333" },
            "campaign_alert",
            new Dictionary<string, string> { ["campaign"] = "Yaz Indirimi" });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Bot_SendBulkNotification_Telegram_ShouldReturnTrue()
    {
        var service = CreateBotService();

        var result = await service.SendBulkNotificationAsync(
            NotificationChannel.Telegram,
            new List<string> { "channel-1", "channel-2" },
            "system_alert",
            new Dictionary<string, string> { ["message"] = "Sistem bakimda" });

        result.Should().BeTrue();
    }
}
