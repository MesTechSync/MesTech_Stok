using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// Ciceksepeti API contract testleri — TDD Dalga 3.
/// WireMock ile API spec'e gore stub'lanir.
/// Tum testler RED: DEV 3 implemente edene kadar Skip.
/// API Spec: https://ciceksepeti.dev
///
/// Auth: x-api-key header
/// Base URL: https://apis.ciceksepeti.com/api/v1/
/// Products: GET /api/v1/Products?page={N}&amp;pageSize=100
/// Stock+Price: PUT /api/v1/Products/stock-and-price
/// Categories: GET /api/v1/Categories
/// Orders: GET /api/v1/Orders?startDate={date}&amp;pageSize=100
/// </summary>
[Trait("Category", "Integration")]
[Trait("Status", "ContractRED")]
[Trait("Platform", "Ciceksepeti")]
public class CiceksepetiAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<CiceksepetiAdapter> _logger;

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["ApiKey"] = "test-ciceksepeti-api-key"
    };

    public CiceksepetiAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<CiceksepetiAdapter>();
    }

    private CiceksepetiAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new CiceksepetiAdapter(httpClient, _logger);
    }

    private CiceksepetiAdapter CreateAdapterWithTimeout(TimeSpan timeout)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout
        };
        return new CiceksepetiAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Adapter'i yapilandirip kullanima hazir hale getirir.
    /// TestConnectionAsync basarili olursa _isConfigured = true olur.
    /// </summary>
    private async Task<CiceksepetiAdapter> CreateConfiguredAdapterAsync()
    {
        // WireMock: TestConnectionAsync icin GET /api/v1/Products?page=1&pageSize=1
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""products"":[],""totalCount"":0}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset(); // Sonraki test icin mock'lari temizle
        return adapter;
    }

    // ══════════════════════════════════════════════════════
    // Section 1 — TestConnectionAsync (5 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task TestConnection_ValidApiKey_ReturnsSuccess()
    {
        // Arrange — GET /api/v1/Products?page=1&pageSize=1 with x-api-key header
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "1")
                .WithHeader("x-api-key", "test-ciceksepeti-api-key")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [{""productCode"":""CS-001"",""productName"":""Test Cicek""}],
                    ""totalCount"": 150
                }"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProductCount.Should().Be(150);
        result.PlatformCode.Should().Be("Ciceksepeti");
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task TestConnection_InvalidApiKey_Returns401()
    {
        // Arrange — 401 Unauthorized response
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""message"":""Unauthorized""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task TestConnection_MissingApiKey_ReturnsErrorWithoutHttpCall()
    {
        // Arrange — empty credential, should fail before HTTP call
        var adapter = CreateAdapter();
        var emptyCredentials = new Dictionary<string, string>
        {
            ["ApiKey"] = ""
        };

        // Act
        var result = await adapter.TestConnectionAsync(emptyCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.HttpStatusCode.Should().BeNull();
        // Verify no HTTP call was made
        _mockServer.LogEntries.Should().BeEmpty();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task TestConnection_Timeout_ReturnsError()
    {
        // Arrange — 10 second delay, 2 second client timeout
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10))
                .WithBody(@"{""products"":[],""totalCount"":0}"));

        var adapter = CreateAdapterWithTimeout(TimeSpan.FromSeconds(2));

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task TestConnection_ServerError500_ReturnsFailure()
    {
        // Arrange — 500 Internal Server Error
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""message"":""Internal Server Error""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(500);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ══════════════════════════════════════════════════════
    // Section 2 — PullProductsAsync (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PullProducts_SinglePage_ReturnsMappedProducts()
    {
        // Arrange — GET /api/v1/Products?page=1&pageSize=100
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [
                        {
                            ""productCode"": ""CS-001"",
                            ""productName"": ""Kirmizi Gul Buketi"",
                            ""stockCode"": ""SKU-CICEK-001"",
                            ""stockQuantity"": 45,
                            ""salesPrice"": 249.90,
                            ""listPrice"": 299.90,
                            ""barcode"": ""8691234567100"",
                            ""description"": ""12 adet kirmizi gul buketi""
                        }
                    ],
                    ""totalCount"": 1
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        var p = products[0];
        p.Name.Should().Be("Kirmizi Gul Buketi");
        p.SKU.Should().Be("SKU-CICEK-001");
        p.Barcode.Should().Be("8691234567100");
        p.Stock.Should().Be(45);
        p.SalePrice.Should().Be(249.90m);
        p.ListPrice.Should().Be(299.90m);
        p.Description.Should().Be("12 adet kirmizi gul buketi");
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PullProducts_MultiplePages_ReturnsAllProducts()
    {
        // Arrange — page 1 and page 2
        var adapter = await CreateConfiguredAdapterAsync();

        // Page 1
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [
                        {""productCode"":""CS-001"",""productName"":""Gul Buketi"",""stockCode"":""SKU-A"",""stockQuantity"":10,""salesPrice"":50.0}
                    ],
                    ""totalCount"": 2
                }"));

        // Page 2
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "2")
                .WithParam("pageSize", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [
                        {""productCode"":""CS-002"",""productName"":""Orkide"",""stockCode"":""SKU-B"",""stockQuantity"":20,""salesPrice"":75.0}
                    ],
                    ""totalCount"": 2
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Gul Buketi");
        products[0].SKU.Should().Be("SKU-A");
        products[1].Name.Should().Be("Orkide");
        products[1].SKU.Should().Be("SKU-B");
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PullProducts_EmptyResult_ReturnsEmptyList()
    {
        // Arrange — empty products array
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [],
                    ""totalCount"": 0
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PullProducts_RateLimit429_RetriesAndReturns()
    {
        // Arrange — WireMock Scenario: first 429 then 200 on retry
        var adapter = await CreateConfiguredAdapterAsync();

        // First request returns 429 Too Many Requests
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "100")
                .UsingGet())
            .InScenario("RateLimit")
            .WillSetStateTo("RetryAllowed")
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "1")
                .WithBody(@"{""message"":""Too Many Requests""}"));

        // Retry returns 200
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("page", "1")
                .WithParam("pageSize", "100")
                .UsingGet())
            .InScenario("RateLimit")
            .WhenStateIs("RetryAllowed")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""products"": [
                        {""productCode"":""CS-001"",""productName"":""Retry Urun"",""stockCode"":""SKU-R"",""stockQuantity"":5,""salesPrice"":30.0}
                    ],
                    ""totalCount"": 1
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        products[0].Name.Should().Be("Retry Urun");
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should retry after 429 rate limit");
    }

    // ══════════════════════════════════════════════════════
    // Section 3 — PushStockUpdate / PushPriceUpdate / PushProduct (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        // Arrange — PUT /api/v1/Products/stock-and-price
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock-and-price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":true,""message"":""Stock updated""}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 100);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PushStockUpdate_BadRequest_ReturnsFalse()
    {
        // Arrange — 400 Bad Request
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock-and-price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":false,""message"":""Invalid stock value""}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, -5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PushPriceUpdate_Success_ReturnsTrue()
    {
        // Arrange — PUT /api/v1/Products/stock-and-price (same endpoint)
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock-and-price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":true,""message"":""Price updated""}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 199.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PushProduct_Success_ReturnsTrue()
    {
        // Arrange — POST /api/v1/Products
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":true,""productId"":""CS-NEW-001""}"));

        var product = new Product
        {
            Name = "Yeni Cicek Aranjmani",
            SKU = "CS-NEW-001",
            Barcode = "8691234567200",
            SalePrice = 179.90m,
            ListPrice = 219.90m,
            Stock = 30,
            CategoryId = Guid.NewGuid(),
            Description = "Ozel tasarim cicek aranjmani"
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════
    // Section 4 — Webhook + Edge Cases (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task ProcessWebhook_OrderCreated_ShouldNotThrow()
    {
        // Arrange — OrderCreated JSON payload
        var adapter = await CreateConfiguredAdapterAsync();
        var payload = @"{
            ""eventType"": ""OrderCreated"",
            ""orderId"": ""ORD-CS-001"",
            ""orderDate"": ""2026-03-08T10:30:00Z"",
            ""totalAmount"": 249.90,
            ""items"": [
                {
                    ""productCode"": ""CS-001"",
                    ""quantity"": 1,
                    ""unitPrice"": 249.90
                }
            ]
        }";

        // Act
        var act = () => adapter.ProcessWebhookPayloadAsync(payload);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task ProcessWebhook_OrderCancelled_ShouldNotThrow()
    {
        // Arrange — OrderCancelled JSON payload
        var adapter = await CreateConfiguredAdapterAsync();
        var payload = @"{
            ""eventType"": ""OrderCancelled"",
            ""orderId"": ""ORD-CS-002"",
            ""cancellationDate"": ""2026-03-08T14:00:00Z"",
            ""reason"": ""Musteri talebi""
        }";

        // Act
        var act = () => adapter.ProcessWebhookPayloadAsync(payload);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task PushProduct_ServerError500_PollyRetries()
    {
        // Arrange — 500 on all attempts, verify retry count >= 2
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Internal Server Error""}"));

        var product = new Product
        {
            Name = "Retry Cicek",
            SKU = "CS-RTR-001",
            Barcode = "8691234567300",
            SalePrice = 99.90m,
            ListPrice = 129.90m,
            Stock = 10,
            CategoryId = Guid.NewGuid(),
            Description = "Polly retry test urunu"
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse();
        _mockServer.LogEntries.Should().HaveCountGreaterThanOrEqualTo(2,
            "Polly should retry on 500 errors");
    }

    [Fact(Skip = "RED: Awaiting DEV 3 CiceksepetiAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    public async Task GetCategories_ReturnsCategoryTree()
    {
        // Arrange — GET /api/v1/Categories with nested category response
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""categories"": [
                        {
                            ""categoryId"": 1001,
                            ""categoryName"": ""Cicekler"",
                            ""parentCategoryId"": null,
                            ""isLeaf"": false,
                            ""subCategories"": [
                                {
                                    ""categoryId"": 2001,
                                    ""categoryName"": ""Gul Buketleri"",
                                    ""parentCategoryId"": 1001,
                                    ""isLeaf"": true,
                                    ""subCategories"": []
                                },
                                {
                                    ""categoryId"": 2002,
                                    ""categoryName"": ""Orkideler"",
                                    ""parentCategoryId"": 1001,
                                    ""isLeaf"": true,
                                    ""subCategories"": []
                                }
                            ]
                        },
                        {
                            ""categoryId"": 1002,
                            ""categoryName"": ""Hediye Sepetleri"",
                            ""parentCategoryId"": null,
                            ""isLeaf"": false,
                            ""subCategories"": [
                                {
                                    ""categoryId"": 2003,
                                    ""categoryName"": ""Cikolata Sepetleri"",
                                    ""parentCategoryId"": 1002,
                                    ""isLeaf"": true,
                                    ""subCategories"": []
                                }
                            ]
                        }
                    ]
                }"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeEmpty();
        categories.Should().HaveCountGreaterThanOrEqualTo(2);

        var cicekler = categories.First(c => c.Name == "Cicekler");
        cicekler.PlatformCategoryId.Should().Be(1001);
        cicekler.ParentId.Should().BeNull();
        cicekler.SubCategories.Should().HaveCount(2);

        var gulBuketleri = cicekler.SubCategories.First(c => c.Name == "Gul Buketleri");
        gulBuketleri.PlatformCategoryId.Should().Be(2001);
        gulBuketleri.ParentId.Should().Be(1001);
    }

    public void Dispose()
    {
        // Fixture is shared and disposed by xUnit
    }
}
