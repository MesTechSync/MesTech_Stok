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
/// TEST 3/4 — PushProduct payload: 16 zorunlu alan doğru tipte mi?
/// Tek test dosyası, her alan için ayrı assertion.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Platform", "Trendyol")]
public class TrendyolPushPayload16FieldTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _http;
    private readonly TrendyolAdapter _sut;
    private string? _body;

    public TrendyolPushPayload16FieldTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _http = new HttpClient(_handler.Object) { BaseAddress = new Uri("https://apigw.trendyol.com") };
        _sut = new TrendyolAdapter(_http, NullLogger<TrendyolAdapter>.Instance, Options.Create(new TrendyolOptions()));

        Setup(HttpStatusCode.OK, "{\"totalElements\":1}");
        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ApiKey"] = "k", ["ApiSecret"] = "s", ["SupplierId"] = "999"
        }).GetAwaiter().GetResult();

        Setup(HttpStatusCode.OK, "{\"batchRequestId\":\"b1\"}");
    }

    public void Dispose() { _http.Dispose(); GC.SuppressFinalize(this); }

    private void Setup(HttpStatusCode code, string content)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content is not null) _body = await req.Content.ReadAsStringAsync();
                return new HttpResponseMessage { StatusCode = code,
                    Content = new StringContent(content, Encoding.UTF8, "application/json") };
            });
    }

    private static Product MakeProduct()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var brand = Brand.Create(tenantId, "TestBrand");
        brand.PlatformMappings = new List<BrandPlatformMapping>
        {
            new() { TenantId = tenantId, BrandId = brand.Id, StoreId = storeId,
                     PlatformType = PlatformType.Trendyol, ExternalBrandId = "5001" }
        };

        var product = Product.Create(tenantId, "SKU-16F", "16 Alan Urun", 199.90m, 100m, Guid.NewGuid());
        product.Barcode = "8690000000001";
        product.ListPrice = 299.90m;
        product.ImageUrl = "https://cdn.trendyol.com/img/test.jpg";
        product.BrandEntity = brand;
        product.SyncStock(75);

        product.PlatformMappings = new List<ProductPlatformMapping>
        {
            new() { TenantId = tenantId, ProductId = product.Id, StoreId = storeId,
                     PlatformType = PlatformType.Trendyol, ExternalCategoryId = "411",
                     PlatformSpecificData = JsonSerializer.Serialize(new {
                         attributes = new[] { new { attributeId = 338, attributeValueId = 100 } }
                     }) }
        };

        return product;
    }

    private JsonElement Item()
    {
        _body.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(_body!);
        return doc.RootElement.GetProperty("items")[0].Clone();
    }

    [Fact]
    public async Task AllSixteenFields_ShouldExistWithCorrectTypes()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();

        // 1. barcode — string
        item.GetProperty("barcode").ValueKind.Should().Be(JsonValueKind.String);
        item.GetProperty("barcode").GetString().Should().Be("8690000000001");

        // 2. title — string
        item.GetProperty("title").ValueKind.Should().Be(JsonValueKind.String);

        // 3. productMainId — string
        item.GetProperty("productMainId").ValueKind.Should().Be(JsonValueKind.String);

        // 4. brandId — number (int)
        item.GetProperty("brandId").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("brandId").GetInt32().Should().Be(5001);

        // 5. categoryId — number (int)
        item.GetProperty("categoryId").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("categoryId").GetInt32().Should().Be(411);

        // 6. quantity — number (int)
        item.GetProperty("quantity").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("quantity").GetInt32().Should().Be(75);

        // 7. stockCode — string
        item.GetProperty("stockCode").ValueKind.Should().Be(JsonValueKind.String);

        // 8. dimensionalWeight — number
        item.GetProperty("dimensionalWeight").ValueKind.Should().Be(JsonValueKind.Number);

        // 9. description — string
        item.GetProperty("description").ValueKind.Should().Be(JsonValueKind.String);

        // 10. currencyType — string "TRY"
        item.GetProperty("currencyType").GetString().Should().Be("TRY");

        // 11. listPrice — number (decimal)
        item.GetProperty("listPrice").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("listPrice").GetDecimal().Should().Be(299.90m);

        // 12. salePrice — number (decimal)
        item.GetProperty("salePrice").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("salePrice").GetDecimal().Should().Be(199.90m);

        // 13. vatRate — number (int), valid Turkish rate
        item.GetProperty("vatRate").ValueKind.Should().Be(JsonValueKind.Number);
        new[] { 0, 1, 8, 10, 18, 20 }.Should().Contain(item.GetProperty("vatRate").GetInt32());

        // 14. cargoCompanyId — number
        item.GetProperty("cargoCompanyId").ValueKind.Should().Be(JsonValueKind.Number);

        // 15. images — array of {url: string}
        var images = item.GetProperty("images");
        images.ValueKind.Should().Be(JsonValueKind.Array);
        images.GetArrayLength().Should().BeGreaterOrEqualTo(1);
        images[0].GetProperty("url").ValueKind.Should().Be(JsonValueKind.String);

        // 16. attributes — array of objects
        var attrs = item.GetProperty("attributes");
        attrs.ValueKind.Should().Be(JsonValueKind.Array);
        attrs.GetArrayLength().Should().BeGreaterOrEqualTo(1);
        attrs[0].GetProperty("attributeId").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public async Task TopLevel_ShouldBeItemsArray()
    {
        await _sut.PushProductAsync(MakeProduct());
        using var doc = JsonDocument.Parse(_body!);
        doc.RootElement.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Prices_ListPriceGreaterOrEqualSalePrice()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("listPrice").GetDecimal()
            .Should().BeGreaterOrEqualTo(item.GetProperty("salePrice").GetDecimal());
    }

    [Fact]
    public async Task BarcodeAndStockCode_ShouldNotBeEmpty()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("barcode").GetString().Should().NotBeNullOrEmpty();
        item.GetProperty("stockCode").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ImageUrl_ShouldStartWithHttps()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("images")[0].GetProperty("url").GetString()
            .Should().StartWith("https://");
    }

    [Fact]
    public async Task CurrencyType_AlwaysTRY()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("currencyType").GetString().Should().Be("TRY");
    }

    [Fact]
    public async Task Quantity_ShouldBeNonNegative()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("quantity").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task AttributeId_ShouldBePositiveInt()
    {
        await _sut.PushProductAsync(MakeProduct());
        var item = Item();
        item.GetProperty("attributes")[0].GetProperty("attributeId").GetInt32().Should().Be(338);
        item.GetProperty("attributes")[0].GetProperty("attributeValueId").GetInt32().Should().Be(100);
    }
}
