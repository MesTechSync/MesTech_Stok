using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV 5 — Trendyol PushProductAsync JSON payload structure validation.
/// Ensures Trendyol API contract: correct JSON types for categoryId (int),
/// brandId (int), images (array of {url}), attributes (array of objects),
/// barcode (string), quantity (int), prices (numeric), Turkish chars preserved.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Platform", "Trendyol")]
public class TrendyolPayloadStructureTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly TrendyolAdapter _sut;

    /// <summary>Captured JSON body from the last HTTP POST request.</summary>
    private string? _capturedRequestBody;

    public TrendyolPayloadStructureTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com")
        };
        var logger = NullLogger<TrendyolAdapter>.Instance;
        var options = Options.Create(new TrendyolOptions());
        _sut = new TrendyolAdapter(_httpClient, logger, options);

        // Configure auth so EnsureConfigured passes
        var credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "test-api-key",
            ["ApiSecret"] = "test-api-secret",
            ["SupplierId"] = "123456"
        };

        // Setup successful connection test response to configure the adapter
        SetupCaptureResponse(HttpStatusCode.OK, "{\"totalElements\":1}");
        _sut.TestConnectionAsync(credentials).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // ═══════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sets up the mock handler to capture POST request body and return the given response.
    /// </summary>
    private void SetupCaptureResponse(HttpStatusCode statusCode, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content is not null)
                    _capturedRequestBody = await req.Content.ReadAsStringAsync();
                return new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };
            });
    }

    /// <summary>
    /// Creates a fully-mapped Product suitable for Trendyol push.
    /// brandId and categoryId are resolved from PlatformMappings.
    /// </summary>
    private static Product CreateValidTrendyolProduct(
        string name = "Test Ürün",
        string sku = "TST-001",
        string barcode = "8680001234567",
        decimal salePrice = 199.99m,
        decimal? listPrice = 249.99m,
        int stock = 50,
        string imageUrl = "https://cdn.trendyol.com/images/test.jpg",
        int platformBrandId = 7891,
        int platformCategoryId = 1234,
        string? platformSpecificData = null)
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var brand = Brand.Create(tenantId, "Test Marka");
        brand.PlatformMappings = new List<BrandPlatformMapping>
        {
            new()
            {
                TenantId = tenantId,
                BrandId = brand.Id,
                StoreId = storeId,
                PlatformType = PlatformType.Trendyol,
                ExternalBrandId = platformBrandId.ToString()
            }
        };

        var product = Product.Create(tenantId, sku, name, salePrice, 100m, Guid.NewGuid());
        product.Barcode = barcode;
        product.ListPrice = listPrice;
        product.ImageUrl = imageUrl;
        product.BrandEntity = brand;
        product.SyncStock(stock);

        product.PlatformMappings = new List<ProductPlatformMapping>
        {
            new()
            {
                TenantId = tenantId,
                ProductId = product.Id,
                StoreId = storeId,
                PlatformType = PlatformType.Trendyol,
                ExternalCategoryId = platformCategoryId.ToString(),
                PlatformSpecificData = platformSpecificData
            }
        };

        return product;
    }

    /// <summary>Parses captured body and returns the first item from items array.</summary>
    private JsonElement GetFirstItem()
    {
        _capturedRequestBody.Should().NotBeNullOrEmpty("PushProductAsync should have sent an HTTP request");
        using var doc = JsonDocument.Parse(_capturedRequestBody!);
        var root = doc.RootElement;
        root.TryGetProperty("items", out var items).Should().BeTrue("payload must have 'items' array");
        items.GetArrayLength().Should().BeGreaterThan(0);
        return items[0].Clone();
    }

    // ═══════════════════════════════════════════
    // Payload Structure Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task Payload_CategoryId_ShouldBeNumber()
    {
        // Arrange
        var product = CreateValidTrendyolProduct(platformCategoryId: 411);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("categoryId", out var categoryId).Should().BeTrue();
        categoryId.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires categoryId as integer, not string");
        categoryId.GetInt32().Should().Be(411);
    }

    [Fact]
    public async Task Payload_BrandId_ShouldBeNumber()
    {
        // Arrange
        var product = CreateValidTrendyolProduct(platformBrandId: 7891);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("brandId", out var brandId).Should().BeTrue();
        brandId.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires brandId as integer, not string");
        brandId.GetInt32().Should().Be(7891);
    }

    [Fact]
    public async Task Payload_Images_ShouldBeArrayOfObjectsWithUrl()
    {
        // Arrange
        const string imgUrl = "https://cdn.trendyol.com/images/product1.jpg";
        var product = CreateValidTrendyolProduct(imageUrl: imgUrl);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("images", out var images).Should().BeTrue();
        images.ValueKind.Should().Be(JsonValueKind.Array,
            "Trendyol API requires images as array of {url} objects");
        images.GetArrayLength().Should().BeGreaterOrEqualTo(1);

        var firstImage = images[0];
        firstImage.ValueKind.Should().Be(JsonValueKind.Object,
            "each image must be an object with 'url' property");
        firstImage.TryGetProperty("url", out var url).Should().BeTrue();
        url.ValueKind.Should().Be(JsonValueKind.String);
        url.GetString().Should().Be(imgUrl);
    }

    [Fact]
    public async Task Payload_Attributes_ShouldBeArrayOfObjects()
    {
        // Arrange — inject attributes via PlatformSpecificData JSON
        var platformData = JsonSerializer.Serialize(new
        {
            attributes = new[]
            {
                new { attributeId = 338, attributeValueId = 4567 },
                new { attributeId = 47, attributeValueId = 112 }
            }
        });
        var product = CreateValidTrendyolProduct(platformSpecificData: platformData);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("attributes", out var attributes).Should().BeTrue();
        attributes.ValueKind.Should().Be(JsonValueKind.Array,
            "Trendyol API requires attributes as array of {attributeId, attributeValueId} objects");
        attributes.GetArrayLength().Should().Be(2);

        var first = attributes[0];
        first.TryGetProperty("attributeId", out var attrId).Should().BeTrue();
        attrId.ValueKind.Should().Be(JsonValueKind.Number,
            "attributeId must be integer");
        attrId.GetInt32().Should().Be(338);

        first.TryGetProperty("attributeValueId", out var attrValueId).Should().BeTrue();
        attrValueId.ValueKind.Should().Be(JsonValueKind.Number,
            "attributeValueId must be integer");
        attrValueId.GetInt32().Should().Be(4567);
    }

    [Fact]
    public async Task Payload_Attributes_CustomValue_ShouldSerializeCorrectly()
    {
        // Arrange — Trendyol supports customAttributeValue for free-text attributes
        var platformData = JsonSerializer.Serialize(new
        {
            attributes = new object[]
            {
                new { attributeId = 338, customAttributeValue = "Kırmızı" }
            }
        });
        var product = CreateValidTrendyolProduct(platformSpecificData: platformData);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("attributes", out var attributes).Should().BeTrue();
        attributes.GetArrayLength().Should().Be(1);

        var attr = attributes[0];
        attr.TryGetProperty("attributeId", out var attrId).Should().BeTrue();
        attrId.GetInt32().Should().Be(338);

        attr.TryGetProperty("customAttributeValue", out var customVal).Should().BeTrue();
        customVal.ValueKind.Should().Be(JsonValueKind.String);
        customVal.GetString().Should().Be("Kırmızı");
    }

    [Fact]
    public async Task Payload_Barcode_ShouldBeString()
    {
        // Arrange
        var product = CreateValidTrendyolProduct(barcode: "8680001234567");
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("barcode", out var barcode).Should().BeTrue();
        barcode.ValueKind.Should().Be(JsonValueKind.String,
            "Trendyol API requires barcode as string (EAN-13/GTIN format)");
        barcode.GetString().Should().Be("8680001234567");
    }

    [Fact]
    public async Task Payload_Barcode_FallsBackToSku_WhenBarcodeNull()
    {
        // Arrange — when product.Barcode is null, adapter should use SKU as barcode
        var product = CreateValidTrendyolProduct(barcode: null!);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("barcode", out var barcode).Should().BeTrue();
        barcode.ValueKind.Should().Be(JsonValueKind.String);
        barcode.GetString().Should().Be("TST-001", "barcode should fall back to SKU when Barcode is null");
    }

    [Fact]
    public async Task Payload_Quantity_ShouldBeInteger()
    {
        // Arrange
        var product = CreateValidTrendyolProduct(stock: 42);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("quantity", out var quantity).Should().BeTrue();
        quantity.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires quantity as integer");
        quantity.GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Payload_Prices_ShouldBeNumeric()
    {
        // Arrange
        var product = CreateValidTrendyolProduct(salePrice: 149.90m, listPrice: 199.90m);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();

        item.TryGetProperty("salePrice", out var salePrice).Should().BeTrue();
        salePrice.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires salePrice as decimal/double");
        salePrice.GetDecimal().Should().Be(149.90m);

        item.TryGetProperty("listPrice", out var listPrice).Should().BeTrue();
        listPrice.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires listPrice as decimal/double");
        listPrice.GetDecimal().Should().Be(199.90m);
    }

    [Fact]
    public async Task Payload_ListPrice_DefaultsToSalePrice_WhenNull()
    {
        // Arrange — when ListPrice is null, adapter uses SalePrice
        var product = CreateValidTrendyolProduct(salePrice: 100m, listPrice: null);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("listPrice", out var listPrice).Should().BeTrue();
        listPrice.GetDecimal().Should().Be(100m,
            "listPrice should default to salePrice when ListPrice is null");
    }

    [Fact]
    public async Task Payload_TurkishCharacters_ShouldBePreserved()
    {
        // Arrange — Turkish İ/ş/ç/ğ/ö/ü characters in product name
        const string turkishName = "Şık Göğüs Çantası İnce Örgülü Ürün";
        var product = CreateValidTrendyolProduct(name: turkishName);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("title", out var title).Should().BeTrue();
        title.ValueKind.Should().Be(JsonValueKind.String);
        var titleStr = title.GetString();
        titleStr.Should().Be(turkishName,
            "Turkish characters (İ/ş/ç/ğ/ö/ü) must be preserved in product title");
        titleStr.Should().Contain("Ş").And.Contain("ö").And.Contain("ü")
            .And.Contain("İ").And.Contain("Ç").And.Contain("ğ");
    }

    [Fact]
    public async Task Payload_VatRate_ShouldBeInteger()
    {
        // Arrange
        var product = CreateValidTrendyolProduct();
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("vatRate", out var vatRate).Should().BeTrue();
        vatRate.ValueKind.Should().Be(JsonValueKind.Number,
            "Trendyol API requires vatRate as integer (0, 1, 10, or 20)");
    }

    [Fact]
    public async Task Payload_TopLevel_ShouldHaveItemsArray()
    {
        // Arrange
        var product = CreateValidTrendyolProduct();
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        _capturedRequestBody.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(_capturedRequestBody!);
        var root = doc.RootElement;
        root.TryGetProperty("items", out var items).Should().BeTrue(
            "Trendyol product push payload must have top-level 'items' array");
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().Be(1, "single product push should have exactly 1 item");
    }

    [Fact]
    public async Task Payload_RequiredFields_ShouldAllBePresent()
    {
        // Arrange
        var product = CreateValidTrendyolProduct();
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert — verify all Trendyol-required fields exist
        var item = GetFirstItem();
        var requiredFields = new[]
        {
            "barcode", "title", "productMainId", "brandId", "categoryId",
            "quantity", "stockCode", "dimensionalWeight", "description",
            "currencyType", "listPrice", "salePrice", "vatRate",
            "cargoCompanyId", "images", "attributes"
        };

        foreach (var field in requiredFields)
        {
            item.TryGetProperty(field, out _).Should().BeTrue(
                $"Trendyol payload must include required field '{field}'");
        }
    }

    [Fact]
    public async Task Payload_CurrencyType_ShouldBeTRY()
    {
        // Arrange
        var product = CreateValidTrendyolProduct();
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("currencyType", out var currency).Should().BeTrue();
        currency.GetString().Should().Be("TRY");
    }

    [Fact]
    public async Task Payload_MultipleImages_FromPlatformSpecificData()
    {
        // Arrange — main image + additional images from PlatformSpecificData
        var platformData = JsonSerializer.Serialize(new
        {
            images = new[] { "https://cdn.img/extra1.jpg", "https://cdn.img/extra2.jpg" }
        });
        var product = CreateValidTrendyolProduct(
            imageUrl: "https://cdn.img/main.jpg",
            platformSpecificData: platformData);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        await _sut.PushProductAsync(product);

        // Assert
        var item = GetFirstItem();
        item.TryGetProperty("images", out var images).Should().BeTrue();
        images.GetArrayLength().Should().Be(3, "1 main + 2 extra images");

        // All images must be objects with 'url' string
        for (int i = 0; i < images.GetArrayLength(); i++)
        {
            var img = images[i];
            img.ValueKind.Should().Be(JsonValueKind.Object);
            img.TryGetProperty("url", out var url).Should().BeTrue();
            url.ValueKind.Should().Be(JsonValueKind.String);
        }
    }

    // ═══════════════════════════════════════════
    // Guard Tests — missing required data
    // ═══════════════════════════════════════════

    [Fact]
    public async Task PushProduct_NoBrandMapping_ReturnsFalse()
    {
        // Arrange — product without BrandEntity/PlatformMappings → brandId=0
        var product = CreateValidTrendyolProduct();
        product.BrandEntity = null;
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        var result = await _sut.PushProductAsync(product);

        // Assert
        result.Should().BeFalse("Trendyol API rejects brandId=0");
    }

    [Fact]
    public async Task PushProduct_NoCategoryMapping_ReturnsFalse()
    {
        // Arrange — product without category platform mapping → categoryId=0
        var product = CreateValidTrendyolProduct();
        product.PlatformMappings = new List<ProductPlatformMapping>();
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        var result = await _sut.PushProductAsync(product);

        // Assert
        result.Should().BeFalse("Trendyol API rejects categoryId=0");
    }

    [Fact]
    public async Task PushProduct_NoImages_ReturnsFalse()
    {
        // Arrange — product without any image
        var product = CreateValidTrendyolProduct(imageUrl: null!);
        SetupCaptureResponse(HttpStatusCode.OK, "{\"batchRequestId\":\"abc-123\"}");

        // Act
        var result = await _sut.PushProductAsync(product);

        // Assert
        result.Should().BeFalse("Trendyol API requires at least 1 product image");
    }
}
