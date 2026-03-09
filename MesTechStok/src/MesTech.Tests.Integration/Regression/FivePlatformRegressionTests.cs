using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// 5-Platform Regression Suite — Dalga 4, Task 8.
/// Verifies that all 5 platform adapters (Trendyol, OpenCart, Ciceksepeti, Hepsiburada, Pazarama)
/// can successfully: TestConnectionAsync, PullProductsAsync, PushStockUpdateAsync,
/// and report the correct PlatformCode.
/// Uses WireMock for HTTP stubs; each test resets stubs to avoid cross-contamination.
/// </summary>
[Trait("Category", "Regression")]
public class FivePlatformRegressionTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;

    private const string TrendyolSupplierId = "REG-SUPPLIER-001";
    private const string HepsiburadaMerchantId = "REG-HB-MERCHANT";
    private const string PazaramaMockToken = "reg-pazarama-mock-token";

    public FivePlatformRegressionTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    // ════════════════════════════════════════════════════════════════
    // Helper: create adapters with fresh HttpClient per test
    // ════════════════════════════════════════════════════════════════

    private TrendyolAdapter CreateTrendyolAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new TrendyolAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<TrendyolAdapter>());
    }

    private Dictionary<string, string> TrendyolCredentials() => new()
    {
        ["ApiKey"] = "test-api-key",
        ["ApiSecret"] = "test-api-secret",
        ["SupplierId"] = TrendyolSupplierId,
        ["BaseUrl"] = _fixture.BaseUrl
    };

    private OpenCartAdapter CreateOpenCartAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new OpenCartAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<OpenCartAdapter>());
    }

    private Dictionary<string, string> OpenCartCredentials() => new()
    {
        ["ApiToken"] = "test-oc-token",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    private CiceksepetiAdapter CreateCiceksepetiAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new CiceksepetiAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<CiceksepetiAdapter>());
    }

    private Dictionary<string, string> CiceksepetiCredentials() => new()
    {
        ["ApiKey"] = "test-cs-api-key",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    private HepsiburadaAdapter CreateHepsiburadaAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new HepsiburadaAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<HepsiburadaAdapter>());
    }

    private Dictionary<string, string> HepsiburadaCredentials() => new()
    {
        ["MerchantId"] = HepsiburadaMerchantId,
        ["ApiKey"] = "test-hb-api-key",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    private PazaramaAdapter CreatePazaramaAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new PazaramaAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<PazaramaAdapter>());
    }

    private Dictionary<string, string> PazaramaCredentials() => new()
    {
        ["PazaramaClientId"] = "test-pz-client-id",
        ["PazaramaClientSecret"] = "test-pz-client-secret",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    // ── Pazarama OAuth2 token stub (required before every Pazarama call) ──

    private void StubPazaramaTokenEndpoint()
    {
        _mockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    access_token = PazaramaMockToken,
                    expires_in = 3600,
                    token_type = "Bearer"
                })));
    }

    /// <summary>
    /// Configures a Pazarama adapter by completing OAuth + brand check,
    /// then resets stubs and re-stubs the token endpoint for subsequent calls.
    /// </summary>
    private async Task<PazaramaAdapter> CreateConfiguredPazaramaAdapterAsync()
    {
        StubPazaramaTokenEndpoint();
        StubPazaramaBrandEndpoint();

        var adapter = CreatePazaramaAdapter();
        await adapter.TestConnectionAsync(PazaramaCredentials());
        _fixture.Reset();
        StubPazaramaTokenEndpoint();

        return adapter;
    }

    private void StubPazaramaBrandEndpoint()
    {
        _mockServer
            .Given(Request.Create().WithPath("/brand/getBrands").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""data"":[],""success"":true}"));
    }

    // ════════════════════════════════════════════════════════════════
    //  TRENDYOL TESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Trendyol_TestConnection_ReturnsSuccess()
    {
        _fixture.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{TrendyolSupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalElements"":5,""totalPages"":1,""content"":[]}"));

        var adapter = CreateTrendyolAdapter();
        var result = await adapter.TestConnectionAsync(TrendyolCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Trendyol");
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Trendyol_PullProducts_ReturnsNonNullList()
    {
        _fixture.Reset();

        // Configure adapter first
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{TrendyolSupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalElements"":1,
                    ""totalPages"":1,
                    ""content"":[{
                        ""title"":""Regression Product"",
                        ""stockCode"":""REG-TY-001"",
                        ""barcode"":""8690001112223"",
                        ""salePrice"":49.90,
                        ""listPrice"":59.90,
                        ""quantity"":10
                    }]
                }"));

        var adapter = CreateTrendyolAdapter();
        await adapter.TestConnectionAsync(TrendyolCredentials());

        var products = await adapter.PullProductsAsync();

        products.Should().NotBeNull();
        products.Should().HaveCountGreaterOrEqualTo(1);
        products[0].SKU.Should().Be("REG-TY-001");
    }

    [Fact]
    public async Task Trendyol_PushStockUpdate_ReturnsTrue()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{TrendyolSupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalElements"":0,""totalPages"":0,""content"":[]}"));

        var adapter = CreateTrendyolAdapter();
        await adapter.TestConnectionAsync(TrendyolCredentials());

        // Stub stock update endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{TrendyolSupplierId}/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""batchRequestId"":""ok""}"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 42);

        result.Should().BeTrue();
    }

    [Fact]
    public void Trendyol_PlatformCode_IsCorrect()
    {
        var adapter = CreateTrendyolAdapter();
        adapter.PlatformCode.Should().Be("Trendyol");
    }

    // ════════════════════════════════════════════════════════════════
    //  OPENCART TESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OpenCart_TestConnection_ReturnsSuccess()
    {
        _fixture.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""total"":3,""data"":[]}"));

        var adapter = CreateOpenCartAdapter();
        var result = await adapter.TestConnectionAsync(OpenCartCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("OpenCart");
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task OpenCart_PullProducts_ReturnsNonNullList()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""total"":1,
                    ""data"":[{
                        ""name"":""OC Regression Product"",
                        ""sku"":""REG-OC-001"",
                        ""price"":""29.90"",
                        ""quantity"":""20""
                    }]
                }"));

        var adapter = CreateOpenCartAdapter();
        await adapter.TestConnectionAsync(OpenCartCredentials());

        var products = await adapter.PullProductsAsync();

        products.Should().NotBeNull();
        products.Should().HaveCountGreaterOrEqualTo(1);
        products[0].SKU.Should().Be("REG-OC-001");
    }

    [Fact]
    public async Task OpenCart_PushStockUpdate_ReturnsTrue()
    {
        _fixture.Reset();
        var productId = Guid.NewGuid();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""total"":0,""data"":[]}"));

        var adapter = CreateOpenCartAdapter();
        await adapter.TestConnectionAsync(OpenCartCredentials());

        // Stub stock update endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/rest/products/{productId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushStockUpdateAsync(productId, 15);

        result.Should().BeTrue();
    }

    [Fact]
    public void OpenCart_PlatformCode_IsCorrect()
    {
        var adapter = CreateOpenCartAdapter();
        adapter.PlatformCode.Should().Be("OpenCart");
    }

    // ════════════════════════════════════════════════════════════════
    //  CICEKSEPETI TESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ciceksepeti_TestConnection_ReturnsSuccess()
    {
        _fixture.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":7,""products"":[]}"));

        var adapter = CreateCiceksepetiAdapter();
        var result = await adapter.TestConnectionAsync(CiceksepetiCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Ciceksepeti");
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Ciceksepeti_PullProducts_ReturnsNonNullList()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"":1,
                    ""products"":[{
                        ""productId"":201,
                        ""productName"":""CS Regression Cicek"",
                        ""stockCode"":""REG-CS-001"",
                        ""barcode"":""8691234500001"",
                        ""salesPrice"":39.90,
                        ""stockQuantity"":12,
                        ""categoryId"":1,
                        ""images"":[]
                    }]
                }"));

        var adapter = CreateCiceksepetiAdapter();
        await adapter.TestConnectionAsync(CiceksepetiCredentials());

        var products = await adapter.PullProductsAsync();

        products.Should().NotBeNull();
        products.Should().HaveCountGreaterOrEqualTo(1);
        products[0].SKU.Should().Be("REG-CS-001");
    }

    [Fact]
    public async Task Ciceksepeti_PushStockUpdate_ReturnsTrue()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""products"":[]}"));

        var adapter = CreateCiceksepetiAdapter();
        await adapter.TestConnectionAsync(CiceksepetiCredentials());

        // Stub stock update endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 30);

        result.Should().BeTrue();
    }

    [Fact]
    public void Ciceksepeti_PlatformCode_IsCorrect()
    {
        var adapter = CreateCiceksepetiAdapter();
        adapter.PlatformCode.Should().Be("Ciceksepeti");
    }

    // ════════════════════════════════════════════════════════════════
    //  HEPSIBURADA TESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Hepsiburada_TestConnection_ReturnsSuccess()
    {
        _fixture.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{HepsiburadaMerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":12,""listings"":[]}"));

        var adapter = CreateHepsiburadaAdapter();
        var result = await adapter.TestConnectionAsync(HepsiburadaCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Hepsiburada");
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Hepsiburada_PullProducts_ReturnsNonNullList()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{HepsiburadaMerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"":1,
                    ""listings"":[{
                        ""hepsiburadaSku"":""HB-SKU-001"",
                        ""merchantSku"":""REG-HB-001"",
                        ""productName"":""HB Regression Product"",
                        ""price"":89.90,
                        ""availableStock"":8,
                        ""listingStatus"":""Active"",
                        ""commissionRate"":8
                    }]
                }"));

        var adapter = CreateHepsiburadaAdapter();
        await adapter.TestConnectionAsync(HepsiburadaCredentials());

        var products = await adapter.PullProductsAsync();

        products.Should().NotBeNull();
        products.Should().HaveCountGreaterOrEqualTo(1);
        products[0].SKU.Should().Be("REG-HB-001");
    }

    [Fact]
    public async Task Hepsiburada_PushStockUpdate_ReturnsTrue()
    {
        _fixture.Reset();

        // Configure adapter
        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{HepsiburadaMerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""listings"":[]}"));

        var adapter = CreateHepsiburadaAdapter();
        await adapter.TestConnectionAsync(HepsiburadaCredentials());

        // Stub stock update endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath("/listings/and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 25);

        result.Should().BeTrue();
    }

    [Fact]
    public void Hepsiburada_PlatformCode_IsCorrect()
    {
        var adapter = CreateHepsiburadaAdapter();
        adapter.PlatformCode.Should().Be("Hepsiburada");
    }

    // ════════════════════════════════════════════════════════════════
    //  PAZARAMA TESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Pazarama_TestConnection_ReturnsSuccess()
    {
        _fixture.Reset();

        StubPazaramaTokenEndpoint();
        StubPazaramaBrandEndpoint();

        var adapter = CreatePazaramaAdapter();
        var result = await adapter.TestConnectionAsync(PazaramaCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Pazarama");
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Pazarama_PullProducts_ReturnsNonNullList()
    {
        _fixture.Reset();

        var adapter = await CreateConfiguredPazaramaAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"":[{
                        ""id"":""a1b2c3d4-0000-0000-0000-000000000001"",
                        ""name"":""PZ Regression Product"",
                        ""displayName"":""PZ Regression Product"",
                        ""code"":""REG-PZ-001"",
                        ""salePrice"":59.90,
                        ""listPrice"":69.90,
                        ""stockCount"":18,
                        ""state"":3,
                        ""groupCode"":""GRP-REG""
                    }],
                    ""success"":true,
                    ""fromCache"":false
                }"));

        var products = await adapter.PullProductsAsync();

        products.Should().NotBeNull();
        products.Should().HaveCountGreaterOrEqualTo(1);
        products[0].SKU.Should().Be("REG-PZ-001");
    }

    [Fact]
    public async Task Pazarama_PushStockUpdate_ReturnsTrue()
    {
        _fixture.Reset();

        var adapter = await CreateConfiguredPazaramaAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/updateStock")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 50);

        result.Should().BeTrue();
    }

    [Fact]
    public void Pazarama_PlatformCode_IsCorrect()
    {
        var adapter = CreatePazaramaAdapter();
        adapter.PlatformCode.Should().Be("Pazarama");
    }

    // ════════════════════════════════════════════════════════════════
    //  CROSS-PLATFORM THEORY: All 5 PlatformCode values
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Trendyol")]
    [InlineData("OpenCart")]
    [InlineData("Ciceksepeti")]
    [InlineData("Hepsiburada")]
    [InlineData("Pazarama")]
    public void AllPlatforms_PlatformCode_IsCorrect(string expected)
    {
        var platformCode = expected switch
        {
            "Trendyol" => CreateTrendyolAdapter().PlatformCode,
            "OpenCart" => CreateOpenCartAdapter().PlatformCode,
            "Ciceksepeti" => CreateCiceksepetiAdapter().PlatformCode,
            "Hepsiburada" => CreateHepsiburadaAdapter().PlatformCode,
            "Pazarama" => CreatePazaramaAdapter().PlatformCode,
            _ => throw new ArgumentOutOfRangeException(nameof(expected))
        };

        platformCode.Should().Be(expected);
    }
}
