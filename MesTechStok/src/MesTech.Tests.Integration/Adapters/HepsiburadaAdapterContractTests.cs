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
/// Hepsiburada API contract testleri — TDD Dalga 3.
/// WireMock ile API spec'e gore stub'lanir.
/// Tum testler RED: DEV 3 implemente edene kadar Skip.
/// API Spec: https://mpop-sit.hepsiburada.com/product/api/
///
/// Auth: Basic Auth header = Authorization: Basic base64(MerchantId:ApiKey:ApiSecret)
/// Base URL: https://mpop-sit.hepsiburada.com/product/api/
/// Listings: GET /product/api/listings?offset={N}&amp;limit=100
/// Stock: PUT /product/api/listings/stock
/// Price: PUT /product/api/listings/price
/// Categories: GET /product/api/categories
/// NO webhooks — polling only
/// Listing statuses: Active, Passive, Banned
/// Pagination: offset-based (offset + limit)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Status", "ContractRED")]
[Trait("Platform", "Hepsiburada")]
public class HepsiburadaAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<HepsiburadaAdapter> _logger;

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["MerchantId"] = "test-merchant-id",
        ["ApiKey"] = "test-hb-api-key",
        ["ApiSecret"] = "test-hb-api-secret"
    };

    public HepsiburadaAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<HepsiburadaAdapter>();
    }

    private HepsiburadaAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        // Basic Auth: base64(MerchantId:ApiKey:ApiSecret)
        var authValue = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("test-merchant-id:test-hb-api-key:test-hb-api-secret"));
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        return new HepsiburadaAdapter(httpClient, _logger);
    }

    private HepsiburadaAdapter CreateAdapterWithTimeout(TimeSpan timeout)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout
        };
        var authValue = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("test-merchant-id:test-hb-api-key:test-hb-api-secret"));
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        return new HepsiburadaAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Adapter'i yapilandirip kullanima hazir hale getirir.
    /// TestConnectionAsync basarili olursa _isConfigured = true olur.
    /// </summary>
    private async Task<HepsiburadaAdapter> CreateConfiguredAdapterAsync()
    {
        // WireMock: TestConnectionAsync icin GET /product/api/listings?offset=0&limit=1
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""listings"":[],""totalCount"":0}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset(); // Sonraki test icin mock'lari temizle
        return adapter;
    }

    // ══════════════════════════════════════════════════════
    // Section 1 — TestConnectionAsync (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task TestConnection_ValidCredentials_ReturnsSuccess()
    {
        // Arrange — GET /product/api/listings?offset=0&limit=1 with Basic Auth header
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "1")
                .WithHeader("Authorization", "Basic *")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [{""hepsiburadaSku"":""HB-001"",""merchantSku"":""MRC-001"",""productName"":""Test Urun""}],
                    ""totalCount"": 350
                }"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProductCount.Should().Be(350);
        result.PlatformCode.Should().Be("Hepsiburada");
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task TestConnection_InvalidAuth_Returns401()
    {
        // Arrange — 401 Unauthorized response
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
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

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task TestConnection_MissingMerchantId_ReturnsErrorWithoutHttpCall()
    {
        // Arrange — empty MerchantId, should fail before HTTP call
        var adapter = CreateAdapter();
        var emptyCredentials = new Dictionary<string, string>
        {
            ["MerchantId"] = "",
            ["ApiKey"] = "test-hb-api-key",
            ["ApiSecret"] = "test-hb-api-secret"
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

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task TestConnection_Timeout_ReturnsError()
    {
        // Arrange — 10 second delay, 2 second client timeout
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10))
                .WithBody(@"{""listings"":[],""totalCount"":0}"));

        var adapter = CreateAdapterWithTimeout(TimeSpan.FromSeconds(2));

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ══════════════════════════════════════════════════════
    // Section 2 — PullProductsAsync (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PullProducts_SinglePage_ReturnsMappedProducts()
    {
        // Arrange — GET /product/api/listings?offset=0&limit=100
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [
                        {
                            ""hepsiburadaSku"": ""HB-001"",
                            ""merchantSku"": ""MRC-001"",
                            ""productName"": ""Samsung Galaxy S24"",
                            ""availableStock"": 50,
                            ""price"": 149.90,
                            ""listingStatus"": ""Active""
                        }
                    ],
                    ""totalCount"": 1
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        var p = products[0];
        p.Name.Should().Be("Samsung Galaxy S24");
        p.SKU.Should().Be("MRC-001");
        p.Stock.Should().Be(50);
        p.SalePrice.Should().Be(149.90m);
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PullProducts_MultiplePages_ReturnsAllProducts()
    {
        // Arrange — offset=0 returns first batch, offset=100 returns second batch
        var adapter = await CreateConfiguredAdapterAsync();

        // First page (offset=0)
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [
                        {""hepsiburadaSku"":""HB-001"",""merchantSku"":""MRC-001"",""productName"":""Urun A"",""availableStock"":10,""price"":50.0,""listingStatus"":""Active""}
                    ],
                    ""totalCount"": 2
                }"));

        // Second page (offset=100)
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "100")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [
                        {""hepsiburadaSku"":""HB-002"",""merchantSku"":""MRC-002"",""productName"":""Urun B"",""availableStock"":20,""price"":75.0,""listingStatus"":""Active""}
                    ],
                    ""totalCount"": 2
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Urun A");
        products[0].SKU.Should().Be("MRC-001");
        products[1].Name.Should().Be("Urun B");
        products[1].SKU.Should().Be("MRC-002");
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PullProducts_EmptyResult_ReturnsEmptyList()
    {
        // Arrange — empty listings array
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [],
                    ""totalCount"": 0
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PullProducts_BannedListing_HandledCorrectly()
    {
        // Arrange — response includes listing with "listingStatus": "Banned"
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [
                        {
                            ""hepsiburadaSku"": ""HB-BAN-001"",
                            ""merchantSku"": ""MRC-BAN-001"",
                            ""productName"": ""Yasakli Urun"",
                            ""availableStock"": 0,
                            ""price"": 99.90,
                            ""listingStatus"": ""Banned""
                        }
                    ],
                    ""totalCount"": 1
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — Banned listings should still be returned (not filtered out)
        products.Should().HaveCount(1);
        products[0].Name.Should().Be("Yasakli Urun");
        products[0].SKU.Should().Be("MRC-BAN-001");
    }

    // ══════════════════════════════════════════════════════
    // Section 3 — PushStockUpdate / PushPriceUpdate (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        // Arrange — PUT /product/api/listings/stock
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings/stock")
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

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushStockUpdate_BadRequest_ReturnsFalse()
    {
        // Arrange — 400 Bad Request
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings/stock")
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

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushPriceUpdate_Success_ReturnsTrue()
    {
        // Arrange — PUT /product/api/listings/price
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings/price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":true,""message"":""Price updated""}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 299.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushPriceUpdate_Failure_ReturnsFalse()
    {
        // Arrange — 400 Bad Request
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings/price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":false,""message"":""Invalid price value""}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, -10m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════
    // Section 4 — PushProduct + Edge Cases (4 tests)
    // ══════════════════════════════════════════════════════

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushProduct_Success_ReturnsTrue()
    {
        // Arrange — POST /product/api/listings
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""isSuccess"":true,""hepsiburadaSku"":""HB-NEW-001""}"));

        var product = new Product
        {
            Name = "Yeni HB Urun",
            SKU = "HB-NEW-001",
            Barcode = "8691234567400",
            SalePrice = 179.90m,
            ListPrice = 219.90m,
            Stock = 30,
            CategoryId = Guid.NewGuid(),
            Description = "Hepsiburada test urunu"
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task PushProduct_ServerError500_PollyRetries()
    {
        // Arrange — 500 on all attempts, verify LogEntries >= 2 (Polly retry)
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Internal Server Error""}"));

        var product = new Product
        {
            Name = "Retry HB Urun",
            SKU = "HB-RTR-001",
            Barcode = "8691234567500",
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

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task ListingStatus_ActivePassiveBanned_AllHandled()
    {
        // Arrange — response with mixed statuses: Active, Passive, Banned
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/listings")
                .WithParam("offset", "0")
                .WithParam("limit", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""listings"": [
                        {
                            ""hepsiburadaSku"": ""HB-ACT-001"",
                            ""merchantSku"": ""MRC-ACT-001"",
                            ""productName"": ""Aktif Urun"",
                            ""availableStock"": 100,
                            ""price"": 199.90,
                            ""listingStatus"": ""Active""
                        },
                        {
                            ""hepsiburadaSku"": ""HB-PAS-001"",
                            ""merchantSku"": ""MRC-PAS-001"",
                            ""productName"": ""Pasif Urun"",
                            ""availableStock"": 0,
                            ""price"": 149.90,
                            ""listingStatus"": ""Passive""
                        },
                        {
                            ""hepsiburadaSku"": ""HB-BAN-001"",
                            ""merchantSku"": ""MRC-BAN-001"",
                            ""productName"": ""Yasakli Urun"",
                            ""availableStock"": 0,
                            ""price"": 299.90,
                            ""listingStatus"": ""Banned""
                        }
                    ],
                    ""totalCount"": 3
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — All statuses should be returned (not filtered out)
        products.Should().HaveCount(3);
        products.Select(p => p.Name).Should().Contain("Aktif Urun");
        products.Select(p => p.Name).Should().Contain("Pasif Urun");
        products.Select(p => p.Name).Should().Contain("Yasakli Urun");
    }

    [Fact(Skip = "RED: Awaiting DEV 3 HepsiburadaAdapter implementation")]
    [Trait("Category", "Integration")]
    [Trait("Status", "ContractRED")]
    [Trait("Platform", "Hepsiburada")]
    public async Task GetCategories_ReturnsHBCategoryList()
    {
        // Arrange — GET /product/api/categories with HB flat category structure
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/product/api/categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""categories"": [
                        {
                            ""categoryId"": 5001,
                            ""categoryName"": ""Elektronik"",
                            ""parentCategoryId"": null
                        },
                        {
                            ""categoryId"": 5010,
                            ""categoryName"": ""Cep Telefonu"",
                            ""parentCategoryId"": 5001
                        },
                        {
                            ""categoryId"": 5020,
                            ""categoryName"": ""Tablet"",
                            ""parentCategoryId"": 5001
                        },
                        {
                            ""categoryId"": 6001,
                            ""categoryName"": ""Ev & Yasam"",
                            ""parentCategoryId"": null
                        }
                    ]
                }"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeEmpty();
        categories.Should().HaveCountGreaterThanOrEqualTo(2);

        var elektronik = categories.First(c => c.Name == "Elektronik");
        elektronik.PlatformCategoryId.Should().Be(5001);
        elektronik.ParentId.Should().BeNull();

        var cepTelefonu = categories.First(c => c.Name == "Cep Telefonu");
        cepTelefonu.PlatformCategoryId.Should().Be(5010);
        cepTelefonu.ParentId.Should().Be(5001);
    }

    public void Dispose()
    {
        // Fixture is shared and disposed by xUnit
    }
}
