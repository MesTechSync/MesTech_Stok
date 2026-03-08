using System.Net.Http;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// TrendyolAdapter entegrasyon testleri.
/// WireMock ile gercek TrendyolAdapter sinifi test edilir.
/// </summary>
[Trait("Category", "Integration")]
public class TrendyolAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<TrendyolAdapter> _logger;

    private const string SupplierId = "12345";

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["ApiKey"] = "test-key",
        ["ApiSecret"] = "test-secret",
        ["SupplierId"] = SupplierId
    };

    public TrendyolAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
    }

    private TrendyolAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new TrendyolAdapter(httpClient, _logger);
    }

    private TrendyolAdapter CreateAdapterWithTimeout(TimeSpan timeout)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout
        };
        return new TrendyolAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Adapter'i yapilandirip kullanima hazir hale getirir.
    /// TestConnectionAsync basarili olursa _isConfigured = true olur.
    /// </summary>
    private async Task<TrendyolAdapter> CreateConfiguredAdapterAsync()
    {
        // WireMock: TestConnectionAsync icin GET /sapigw/suppliers/{id}/products?page=0&size=1
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalElements"":0,""totalPages"":0,""page"":0}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset(); // Sonraki test icin mock'lari temizle
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. TestConnectionAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_Success_ReturnsIsSuccessTrue()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [{""barcode"":""123"",""title"":""Test""}],
                    ""totalElements"": 42,
                    ""totalPages"": 1,
                    ""page"": 0
                }"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProductCount.Should().Be(42);
        result.StoreName.Should().Contain(SupplierId);
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestConnectionAsync_Unauthorized401_ReturnsFailureWithErrorMessage()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"":""Unauthorized""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.ErrorMessage.Should().Contain("Yetkisiz");
    }

    [Fact]
    public async Task TestConnectionAsync_MissingCredentials_ReturnsErrorWithoutHttpCall()
    {
        // Arrange — no WireMock stub needed, should fail before HTTP call
        var adapter = CreateAdapter();
        var emptyCredentials = new Dictionary<string, string>
        {
            ["ApiKey"] = "",
            ["ApiSecret"] = "test-secret",
            ["SupplierId"] = "12345"
        };

        // Act
        var result = await adapter.TestConnectionAsync(emptyCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.ErrorMessage.Should().Contain("zorunlu");
        result.HttpStatusCode.Should().BeNull();
        // Verify no HTTP call was made
        _mockServer.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task TestConnectionAsync_MissingApiKeyKey_ReturnsErrorWithoutHttpCall()
    {
        // Arrange — credential dictionary without ApiKey key at all
        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["ApiSecret"] = "test-secret",
            ["SupplierId"] = "12345"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zorunlu");
    }

    [Fact]
    public async Task TestConnectionAsync_Timeout_ReturnsErrorMessage()
    {
        // Arrange — 10 second delay, 2 second client timeout
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10))
                .WithBody(@"{""content"":[],""totalElements"":0,""totalPages"":0}"));

        var adapter = CreateAdapterWithTimeout(TimeSpan.FromSeconds(2));

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.ErrorMessage.Should().Contain("zaman asimi");
    }

    // ══════════════════════════════════════
    // 2. PushProductAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushProductAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/v2/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""batchRequestId"":""batch-001""}"));

        var product = new Product
        {
            Name = "Test Urun",
            SKU = "TST-001",
            Barcode = "8691234567005",
            SalePrice = 99.90m,
            ListPrice = 129.90m,
            Stock = 50,
            CategoryId = Guid.NewGuid(),
            Description = "Test aciklama",
            CurrencyCode = "TRY",
            TaxRate = 0.18m
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushProductAsync_ApiError400_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/v2/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""message"":""Invalid barcode""}]}"));

        var product = new Product
        {
            Name = "Bad Product",
            SKU = "BAD-001",
            SalePrice = 10m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PushProductAsync_ServerError500_PollyRetriesThenReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // WireMock her POST icin 500 donecek — Polly 3 retry yapacak (toplam 4 istek)
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/v2/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Internal Server Error""}"));

        var product = new Product
        {
            Name = "Retry Product",
            SKU = "RTR-001",
            SalePrice = 25m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse();
        // Polly retries: 1 initial + 3 retries = 4 requests minimum
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should retry on 500 errors");
    }

    // ══════════════════════════════════════
    // 3. PullProductsAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_SinglePage_ReturnsCorrectlyMappedProducts()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {
                            ""barcode"": ""8691234567005"",
                            ""title"": ""Mavi Tisort"",
                            ""stockCode"": ""TST-001"",
                            ""quantity"": 75,
                            ""salePrice"": 149.90,
                            ""listPrice"": 199.90,
                            ""description"": ""Pamuklu mavi tisort""
                        }
                    ],
                    ""totalElements"": 1,
                    ""totalPages"": 1,
                    ""page"": 0
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        var p = products[0];
        p.Name.Should().Be("Mavi Tisort");
        p.SKU.Should().Be("TST-001");
        p.Barcode.Should().Be("8691234567005");
        p.Stock.Should().Be(75);
        p.SalePrice.Should().Be(149.90m);
        p.ListPrice.Should().Be(199.90m);
        p.Description.Should().Be("Pamuklu mavi tisort");
    }

    [Fact]
    public async Task PullProductsAsync_MultiplePages_ReturnsAllProducts()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Page 0
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {""barcode"":""BC-001"",""title"":""Urun A"",""stockCode"":""SKU-A"",""quantity"":10,""salePrice"":50.0}
                    ],
                    ""totalElements"": 2,
                    ""totalPages"": 2,
                    ""page"": 0
                }"));

        // Page 1
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "1")
                .WithParam("size", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {""barcode"":""BC-002"",""title"":""Urun B"",""stockCode"":""SKU-B"",""quantity"":20,""salePrice"":75.0}
                    ],
                    ""totalElements"": 2,
                    ""totalPages"": 2,
                    ""page"": 1
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Urun A");
        products[0].SKU.Should().Be("SKU-A");
        products[1].Name.Should().Be("Urun B");
        products[1].SKU.Should().Be("SKU-B");
    }

    [Fact]
    public async Task PullProductsAsync_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [],
                    ""totalElements"": 0,
                    ""totalPages"": 0,
                    ""page"": 0
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task PullProductsAsync_ApiError_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithBody(@"{""error"":""Forbidden""}"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 4. PushStockUpdateAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""batchRequestId"":""stock-batch-001""}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 100);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdateAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(@"{""errors"":[{""message"":""Invalid barcode""}]}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 50);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 5. PushPriceUpdateAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""batchRequestId"":""price-batch-001""}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 299.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushPriceUpdateAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(@"{""errors"":[{""message"":""Price must be positive""}]}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 0m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 6. Unconfigured Adapter Test
    // ══════════════════════════════════════

    [Fact]
    public async Task UnconfiguredAdapter_PushProduct_ThrowsInvalidOperationException()
    {
        // Arrange — adapter created without calling TestConnectionAsync
        var adapter = CreateAdapter();
        var product = new Product
        {
            Name = "Test",
            SKU = "SKU-001",
            SalePrice = 10m,
            CategoryId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => adapter.PushProductAsync(product);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    [Fact]
    public async Task UnconfiguredAdapter_PullProducts_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        var act = () => adapter.PullProductsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UnconfiguredAdapter_PushStockUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        var act = () => adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UnconfiguredAdapter_PushPriceUpdate_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        var act = () => adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        // Fixture is shared and disposed by xUnit
    }
}
