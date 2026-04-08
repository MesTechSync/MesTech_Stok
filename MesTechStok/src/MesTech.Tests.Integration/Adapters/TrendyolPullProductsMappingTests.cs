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
/// DEV 5 — TrendyolAdapter.PullProductsAsync mapping doğrulaması.
/// Gerçek Trendyol API response formatıyla WireMock testi.
/// KÇ-12: brandId/categoryId FK mapping, çoklu images, vatRate, productMainId.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Trendyol")]
public class TrendyolPullProductsMappingTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<TrendyolAdapter> _logger;

    private const string SupplierId = "99001";

    private static readonly Dictionary<string, string> Credentials = new()
    {
        ["ApiKey"] = "map-test-key",
        ["ApiSecret"] = "map-test-secret",
        ["SupplierId"] = SupplierId
    };

    /// <summary>
    /// Full Trendyol product response with ALL fields the real API returns.
    /// brandId (int), categoryId (int), images (array), vatRate (int),
    /// productMainId, color, gender — gerçek API'den 1:1.
    /// </summary>
    private const string FullProductResponse = """
    {
        "totalElements": 2,
        "totalPages": 1,
        "page": 0,
        "size": 50,
        "content": [
            {
                "barcode": "8691234567001",
                "title": "Pamuklu Oversize T-Shirt Beyaz",
                "stockCode": "TY-TSH-001",
                "productMainId": "TY-TSH-001",
                "brandId": 102345,
                "categoryId": 1081,
                "pimCategoryId": 1081,
                "quantity": 200,
                "salePrice": 149.90,
                "listPrice": 249.90,
                "currencyType": "TRY",
                "vatRate": 10,
                "description": "Yuzde yuz pamuk oversize kesim beyaz tisort.",
                "images": [
                    { "url": "https://cdn.trendyol.com/img/ty-tsh-001-1.jpg" },
                    { "url": "https://cdn.trendyol.com/img/ty-tsh-001-2.jpg" },
                    { "url": "https://cdn.trendyol.com/img/ty-tsh-001-3.jpg" }
                ],
                "color": "Beyaz",
                "gender": "Unisex"
            },
            {
                "barcode": "8691234567005",
                "title": "Paslanmaz Celik Termos 500ml",
                "stockCode": "TY-KT-005",
                "productMainId": "TY-KT-005",
                "brandId": 102348,
                "categoryId": 2100,
                "pimCategoryId": 2100,
                "quantity": 0,
                "salePrice": 199.90,
                "listPrice": 299.90,
                "currencyType": "TRY",
                "vatRate": 18,
                "description": "304 paslanmaz celik termos.",
                "images": [
                    { "url": "https://cdn.trendyol.com/img/ty-kt-005-1.jpg" }
                ],
                "color": "Gri",
                "gender": "Unisex"
            }
        ]
    }
    """;

    public TrendyolPullProductsMappingTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
    }

    private TrendyolAdapter CreateAdapter()
    {
        var opts = Options.Create(new TrendyolOptions
        {
            ProductionBaseUrl = _fixture.BaseUrl,
            UseSandbox = false
        });
        return new TrendyolAdapter(new HttpClient(), _logger, opts);
    }

    private async Task<TrendyolAdapter> CreateConfiguredAdapterAsync()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/integration/product/sellers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"content":[],"totalElements":0,"totalPages":0,"page":0}"""));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(Credentials);
        _fixture.Reset();
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. brandId → Notes string (FK mapping gap kanıtı)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_BrandId_ShouldBeCapturedInNotesAsJson()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — brandId adapter'da Notes'a JSON formatında yazılıyor
        products.Should().HaveCount(2);
        products[0].Notes.Should().Contain("trendyolBrandId",
            "adapter should capture Trendyol brandId in Notes JSON");
        products[0].Notes.Should().Contain("102345");
        products[1].Notes.Should().Contain("102348");
    }

    [Fact]
    public async Task PullProducts_BrandId_NotMappedToEntityFK_DocumentsGap()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — BrandEntity is null: brandId(int) → Brand(Guid FK) mapping yok
        // Bu test gap'i belgeler: TY-MAP-001
        products[0].BrandEntity.Should().BeNull(
            "PullProducts does NOT resolve brandId to BrandEntity FK — gap TY-MAP-001");
    }

    // ══════════════════════════════════════
    // 2. categoryId → Notes string (FK mapping gap kanıtı)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_CategoryId_ShouldBeCapturedInNotesAsJson()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — adapter categoryId'yi Notes JSON'a yazıyor
        products[0].Notes.Should().Contain("trendyolCategoryId",
            "adapter should capture categoryId in Notes JSON");
        products[0].Notes.Should().Contain("1081");
    }

    // ══════════════════════════════════════
    // 3. images — sadece ilk resim, çoklu kayıp
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_Images_FirstImageMappedToImageUrl()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — Product 1: ilk resim ImageUrl'e, tüm resimler Notes JSON'a
        products[0].ImageUrl.Should().Be("https://cdn.trendyol.com/img/ty-tsh-001-1.jpg",
            "first image should be mapped to ImageUrl");
    }

    [Fact]
    public async Task PullProducts_AllImages_StoredInNotesJson()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — tüm 3 resim Notes JSON'da korunuyor
        products[0].Notes.Should().Contain("ty-tsh-001-1.jpg");
        products[0].Notes.Should().Contain("ty-tsh-001-2.jpg");
        products[0].Notes.Should().Contain("ty-tsh-001-3.jpg",
            "all 3 images should be stored in Notes JSON");
    }

    [Fact]
    public async Task PullProducts_SingleImage_ShouldMapCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — Product 2: tek resim
        products[1].ImageUrl.Should().Be("https://cdn.trendyol.com/img/ty-kt-005-1.jpg");
    }

    // ══════════════════════════════════════
    // 4. vatRate → TaxRate (/ 100)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_VatRate_ShouldMapToTaxRateDecimal()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — vatRate:10 → TaxRate:0.10, vatRate:18 → TaxRate:0.18
        products[0].TaxRate.Should().Be(0.10m,
            "Trendyol vatRate 10 should map to TaxRate 0.10 (divided by 100)");
        products[1].TaxRate.Should().Be(0.18m,
            "Trendyol vatRate 18 should map to TaxRate 0.18");
    }

    // ══════════════════════════════════════
    // 5. productMainId → Code
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_ProductMainId_ShouldMapToCode()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products[0].Code.Should().Be("TY-TSH-001",
            "productMainId should map to Product.Code");
        products[1].Code.Should().Be("TY-KT-005");
    }

    // ══════════════════════════════════════
    // 6. Temel alanlar (barcode, title, stockCode, price, stock)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProducts_CoreFields_ShouldMapCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupProductsEndpoint(FullProductResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — Product 1
        products[0].Name.Should().Be("Pamuklu Oversize T-Shirt Beyaz");
        products[0].SKU.Should().Be("TY-TSH-001");
        products[0].Barcode.Should().Be("8691234567001");
        products[0].SalePrice.Should().Be(149.90m);
        products[0].ListPrice.Should().Be(249.90m);
        products[0].Stock.Should().Be(200);
        products[0].Description.Should().Contain("pamuk");

        // Assert — Product 2: out of stock
        products[1].Stock.Should().Be(0);
        products[1].SalePrice.Should().Be(199.90m);
    }

    // ══════════════════════════════════════
    // Helper
    // ══════════════════════════════════════

    private void SetupProductsEndpoint(string responseBody)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/integration/product/sellers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));
    }

    public void Dispose() { }
}
