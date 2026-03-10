using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Scraping;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Tests.Unit.Scraping;

public class ProductScraperServiceTests
{
    // ─── Platform Detection ─────────────────────────────────────────

    [Fact]
    public void DetectPlatform_Trendyol_ReturnsTrendyol()
    {
        var result = ProductScraperService.DetectPlatform("trendyol.com");
        result.Should().Be("Trendyol");
    }

    [Fact]
    public void DetectPlatform_TrendyolSubdomain_ReturnsTrendyol()
    {
        var result = ProductScraperService.DetectPlatform("www.trendyol.com");
        result.Should().Be("Trendyol");
    }

    [Fact]
    public void DetectPlatform_TrendyolShortlink_ReturnsTrendyol()
    {
        var result = ProductScraperService.DetectPlatform("ty.gl");
        result.Should().Be("Trendyol");
    }

    [Fact]
    public void DetectPlatform_Hepsiburada_ReturnsHepsiburada()
    {
        var result = ProductScraperService.DetectPlatform("hepsiburada.com");
        result.Should().Be("Hepsiburada");
    }

    [Fact]
    public void DetectPlatform_HepsiburadaMobile_ReturnsHepsiburada()
    {
        var result = ProductScraperService.DetectPlatform("m.hepsiburada.com");
        result.Should().Be("Hepsiburada");
    }

    [Fact]
    public void DetectPlatform_N11_ReturnsN11()
    {
        var result = ProductScraperService.DetectPlatform("www.n11.com");
        result.Should().Be("N11");
    }

    [Fact]
    public void DetectPlatform_Ciceksepeti_ReturnsCiceksepeti()
    {
        var result = ProductScraperService.DetectPlatform("ciceksepeti.com");
        result.Should().Be("Ciceksepeti");
    }

    [Fact]
    public void DetectPlatform_Pazarama_ReturnsPazarama()
    {
        var result = ProductScraperService.DetectPlatform("pazarama.com");
        result.Should().Be("Pazarama");
    }

    [Fact]
    public void DetectPlatform_Unknown_ReturnsNull()
    {
        var result = ProductScraperService.DetectPlatform("amazon.com");
        result.Should().BeNull();
    }

    // ─── Product ID Extraction ──────────────────────────────────────

    [Fact]
    public void ExtractProductId_Trendyol_ExtractsNumericId()
    {
        var uri = new Uri("https://www.trendyol.com/apple/iphone-15-p-123456");
        var result = ProductScraperService.ExtractProductId("Trendyol", uri);
        result.Should().Be("123456");
    }

    [Fact]
    public void ExtractProductId_Hepsiburada_ExtractsAlphanumericId()
    {
        var uri = new Uri("https://www.hepsiburada.com/urun-adi-p-HBCV000001");
        var result = ProductScraperService.ExtractProductId("Hepsiburada", uri);
        result.Should().Be("HBCV000001");
    }

    [Fact]
    public void ExtractProductId_N11_ExtractsLastSegmentId()
    {
        var uri = new Uri("https://www.n11.com/urun/urun-adi-789012");
        var result = ProductScraperService.ExtractProductId("N11", uri);
        result.Should().Be("789012");
    }

    [Fact]
    public void ExtractProductId_Ciceksepeti_ExtractsLastSegmentId()
    {
        var uri = new Uri("https://www.ciceksepeti.com/urun/detay/555999");
        var result = ProductScraperService.ExtractProductId("Ciceksepeti", uri);
        result.Should().Be("555999");
    }

    [Fact]
    public void ExtractProductId_Pazarama_ExtractsLastSegmentId()
    {
        var uri = new Uri("https://www.pazarama.com/urun/urun-adi-112233");
        var result = ProductScraperService.ExtractProductId("Pazarama", uri);
        result.Should().Be("112233");
    }

    // ─── ScrapeFromUrlAsync Null/Invalid Guards ─────────────────────

    [Fact]
    public async Task ScrapeFromUrl_EmptyUrl_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.ScrapeFromUrlAsync("");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_NullUrl_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.ScrapeFromUrlAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_InvalidUrl_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.ScrapeFromUrlAsync("not-a-url");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_UnsupportedPlatform_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.ScrapeFromUrlAsync("https://www.amazon.com/product/123");
        result.Should().BeNull();
    }

    // ─── ScrapeFromUrlAsync with Mock HTTP ──────────────────────────

    [Fact]
    public async Task ScrapeFromUrl_TrendyolSuccess_ReturnsScrapedProduct()
    {
        var json = JsonSerializer.Serialize(new
        {
            name = "Test Product",
            price = 99.90,
            imageUrl = "https://img.trendyol.com/test.jpg",
            barcode = "8680001234567",
            brand = "TestBrand",
            categoryPath = "Elektronik > Telefon",
            description = "Test description"
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler);

        var result = await service.ScrapeFromUrlAsync("https://www.trendyol.com/brand/product-p-123456");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Product");
        result.Price.Should().Be(99.90m);
        result.Platform.Should().Be("Trendyol");
        result.ImageUrl.Should().Be("https://img.trendyol.com/test.jpg");
        result.Barcode.Should().Be("8680001234567");
        result.Brand.Should().Be("TestBrand");
        result.CategoryPath.Should().Be("Elektronik > Telefon");
        result.Description.Should().Be("Test description");
    }

    [Fact]
    public async Task ScrapeFromUrl_ApiReturns404_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "");
        var service = CreateService(handler);

        var result = await service.ScrapeFromUrlAsync("https://www.trendyol.com/brand/product-p-999999");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_ApiReturnsInvalidJson_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "not-json");
        var service = CreateService(handler);

        var result = await service.ScrapeFromUrlAsync("https://www.trendyol.com/brand/product-p-123456");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_ApiReturnsMissingTitle_ReturnsNull()
    {
        var json = JsonSerializer.Serialize(new { price = 10.0 });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler);

        var result = await service.ScrapeFromUrlAsync("https://www.trendyol.com/brand/product-p-123456");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ScrapeFromUrl_HepsiburadaSuccess_ReturnsScrapedProduct()
    {
        var json = JsonSerializer.Serialize(new
        {
            title = "HB Product",
            salePrice = 149.99,
            sku = "HB-SKU-001",
            brandName = "HBBrand"
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler);

        var result = await service.ScrapeFromUrlAsync("https://www.hepsiburada.com/urun-adi-p-HBCV000001");

        result.Should().NotBeNull();
        result!.Title.Should().Be("HB Product");
        result.Price.Should().Be(149.99m);
        result.Platform.Should().Be("Hepsiburada");
        result.Barcode.Should().Be("HB-SKU-001");
        result.Brand.Should().Be("HBBrand");
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static ProductScraperService CreateService(HttpMessageHandler? handler = null)
    {
        var httpClient = handler is not null
            ? new HttpClient(handler)
            : new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        return new ProductScraperService(httpClient, NullLogger<ProductScraperService>.Instance);
    }

    /// <summary>
    /// Test double: returns a fixed HTTP response for any request.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
