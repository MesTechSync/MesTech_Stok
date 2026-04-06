using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// TEST 1/4 — PullProducts → Product entity mapping.
/// 5 ürün fixture (golden file formatı), tüm alanlar doğrulanır.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Trendyol")]
public class TrendyolPullProductsEntityMappingTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<TrendyolAdapter> _logger;
    private const string SupplierId = "91030";

    private static readonly Dictionary<string, string> Creds = new()
    {
        ["ApiKey"] = "entity-key", ["ApiSecret"] = "entity-secret", ["SupplierId"] = SupplierId
    };

    /// <summary>5 ürün fixture — gerçek Trendyol product list response formatı.</summary>
    private const string FiveProductsResponse = """
    {
        "totalElements": 5, "totalPages": 1, "page": 0, "size": 50,
        "content": [
            {
                "barcode": "8691234567001", "title": "Pamuklu Oversize T-Shirt Beyaz",
                "stockCode": "TY-TSH-001", "productMainId": "TY-TSH-001",
                "brandId": 102345, "categoryId": 1081, "pimCategoryId": 1081,
                "quantity": 200, "salePrice": 149.90, "listPrice": 249.90,
                "currencyType": "TRY", "vatRate": 10,
                "description": "Yuzde yuz pamuk oversize kesim beyaz tisort.",
                "images": [
                    {"url": "https://cdn.trendyol.com/img/tsh-1.jpg"},
                    {"url": "https://cdn.trendyol.com/img/tsh-2.jpg"}
                ],
                "color": "Beyaz", "gender": "Unisex"
            },
            {
                "barcode": "8691234567002", "title": "Slim Fit Jean Pantolon Koyu Mavi",
                "stockCode": "TY-JN-002", "productMainId": "TY-JN-002",
                "brandId": 102345, "categoryId": 1082, "pimCategoryId": 1082,
                "quantity": 85, "salePrice": 399.90, "listPrice": 599.90,
                "currencyType": "TRY", "vatRate": 10,
                "description": "Slim fit erkek jean pantolon.",
                "images": [{"url": "https://cdn.trendyol.com/img/jn-1.jpg"}],
                "color": "Koyu Mavi", "gender": "Erkek"
            },
            {
                "barcode": "8691234567003", "title": "Spor Ayakkabi Hafif Kosu",
                "stockCode": "TY-SH-003", "productMainId": "TY-SH-003",
                "brandId": 102346, "categoryId": 1095, "pimCategoryId": 1095,
                "quantity": 42, "salePrice": 749.90, "listPrice": 999.90,
                "currencyType": "TRY", "vatRate": 10,
                "description": "Hafif taban kosu ayakkabisi.",
                "images": [{"url": "https://cdn.trendyol.com/img/sh-1.jpg"}],
                "color": "Siyah", "gender": "Erkek"
            },
            {
                "barcode": "8691234567004", "title": "Kadife Kirlent Kilifi 45x45",
                "stockCode": "TY-HM-004", "productMainId": "TY-HM-004",
                "brandId": 102347, "categoryId": 2050, "pimCategoryId": 2050,
                "quantity": 500, "salePrice": 69.90, "listPrice": 99.90,
                "currencyType": "TRY", "vatRate": 10,
                "description": "Kadife kirlent kilifi.",
                "images": [{"url": "https://cdn.trendyol.com/img/hm-1.jpg"}],
                "color": "Lacivert"
            },
            {
                "barcode": "8691234567005", "title": "Paslanmaz Celik Termos 500ml",
                "stockCode": "TY-KT-005", "productMainId": "TY-KT-005",
                "brandId": 102348, "categoryId": 2100, "pimCategoryId": 2100,
                "quantity": 0, "salePrice": 199.90, "listPrice": 299.90,
                "currencyType": "TRY", "vatRate": 18,
                "description": "304 paslanmaz celik termos.",
                "images": [{"url": "https://cdn.trendyol.com/img/kt-1.jpg"}],
                "color": "Gri"
            }
        ]
    }
    """;

    public TrendyolPullProductsEntityMappingTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
    }

    private async Task<TrendyolAdapter> ConfigureAsync()
    {
        _server.Given(Request.Create()
                .WithPath($"/integration/product/sellers/{SupplierId}/products")
                .WithParam("page", "0").WithParam("size", "1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"content":[],"totalElements":0,"totalPages":0,"page":0}"""));
        var adapter = new TrendyolAdapter(new HttpClient(), _logger,
            Options.Create(new TrendyolOptions { ProductionBaseUrl = _fixture.BaseUrl, UseSandbox = false }));
        await adapter.TestConnectionAsync(Creds);
        _fixture.Reset();
        return adapter;
    }

    private void SetupProducts()
    {
        _server.Given(Request.Create()
                .WithPath($"/integration/product/sellers/{SupplierId}/products")
                .WithParam("page", "0").WithParam("size", "50").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(FiveProductsResponse));
    }

    [Fact]
    public async Task Pull_ShouldReturn5Products()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var products = await adapter.PullProductsAsync();
        products.Should().HaveCount(5);
    }

    [Fact]
    public async Task Pull_Product1_NameSkuBarcode()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[0];
        p.Name.Should().Be("Pamuklu Oversize T-Shirt Beyaz");
        p.SKU.Should().Be("TY-TSH-001");
        p.Barcode.Should().Be("8691234567001");
    }

    [Fact]
    public async Task Pull_Product1_Prices()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[0];
        p.SalePrice.Should().Be(149.90m);
        p.ListPrice.Should().Be(249.90m);
    }

    [Fact]
    public async Task Pull_Product1_StockAndTax()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[0];
        p.Stock.Should().Be(200);
        p.TaxRate.Should().Be(0.10m, "vatRate 10 → 0.10");
    }

    [Fact]
    public async Task Pull_Product1_ImageAndCode()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[0];
        p.ImageUrl.Should().Be("https://cdn.trendyol.com/img/tsh-1.jpg");
        p.Code.Should().Be("TY-TSH-001", "productMainId → Code");
    }

    [Fact]
    public async Task Pull_Product1_NotesJsonContainsBrandAndCategory()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[0];
        p.Notes.Should().Contain("102345").And.Contain("1081");
    }

    [Fact]
    public async Task Pull_Product5_OutOfStock()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[4];
        p.Stock.Should().Be(0);
        p.SKU.Should().Be("TY-KT-005");
        p.TaxRate.Should().Be(0.18m, "vatRate 18 → 0.18");
    }

    [Fact]
    public async Task Pull_Product3_DifferentBrand()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var p = (await adapter.PullProductsAsync())[2];
        p.Notes.Should().Contain("102346", "brandId should differ from product 1");
        p.SalePrice.Should().Be(749.90m);
    }

    [Fact]
    public async Task Pull_AllProducts_DescriptionNotNull()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var products = await adapter.PullProductsAsync();
        foreach (var p in products)
            p.Description.Should().NotBeNullOrEmpty($"product {p.SKU} should have description");
    }

    [Fact]
    public async Task Pull_AllProducts_BarcodesUnique()
    {
        var adapter = await ConfigureAsync();
        SetupProducts();
        var products = await adapter.PullProductsAsync();
        var barcodes = products.Select(p => p.Barcode).ToList();
        barcodes.Should().OnlyHaveUniqueItems("each product should have a unique barcode");
    }

    public void Dispose() { }
}
