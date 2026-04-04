using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class FetchProductFromPlatformHandlerTests
{
    private readonly Mock<IProductScraperService> _scraperService = new();
    private readonly Mock<ILogger<FetchProductFromPlatformHandler>> _logger = new();

    private FetchProductFromPlatformHandler CreateHandler() =>
        new(_scraperService.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidUrl_ShouldReturnScrapedProduct()
    {
        var expectedDto = new ScrapedProductDto(
            "Test Product", 99.99m, "https://img.test/1.jpg",
            "8680000000001", "Trendyol", "Elektronik > Telefon",
            "TestBrand", "Test description");

        _scraperService
            .Setup(s => s.ScrapeFromUrlAsync("https://trendyol.com/p-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var handler = CreateHandler();
        var query = new FetchProductFromPlatformQuery("https://trendyol.com/p-123");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.Platform.Should().Be("Trendyol");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnNull()
    {
        _scraperService
            .Setup(s => s.ScrapeFromUrlAsync("https://trendyol.com/p-invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScrapedProductDto?)null);

        var handler = CreateHandler();
        var query = new FetchProductFromPlatformQuery("https://trendyol.com/p-invalid");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallScraperServiceWithCorrectUrl()
    {
        var url = "https://hepsiburada.com/product-abc";
        var handler = CreateHandler();
        var query = new FetchProductFromPlatformQuery(url);

        await handler.Handle(query, CancellationToken.None);

        _scraperService.Verify(s => s.ScrapeFromUrlAsync(url, It.IsAny<CancellationToken>()), Times.Once);
    }
}
