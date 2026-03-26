using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// PazaramaAdapter WireMock contract testleri.
/// 2-arg constructor (httpClient, logger). Adapter icinde ConfigureAuth
/// kendi OAuth2AuthProvider'ini olusturur. BaseUrl credentials ile override edilir.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Pazarama")]
public class PazaramaAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<PazaramaAdapter> _logger;

    private const string TestClientId = "test-pazarama-client-id";
    private const string TestClientSecret = "test-pazarama-client-secret";
    private const string MockAccessToken = "mock-pazarama-access-token-xyz";

    public PazaramaAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<PazaramaAdapter>();
    }

    private PazaramaAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        var mockFactory = new Moq.Mock<IHttpClientFactory>();
        mockFactory
            .Setup(f => f.CreateClient(Moq.It.IsAny<string>()))
            .Returns(() => new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl), Timeout = TimeSpan.FromSeconds(30) });
        return new PazaramaAdapter(httpClient, _logger, mockFactory.Object);
    }

    private Dictionary<string, string> GetValidCredentials()
    {
        return new Dictionary<string, string>
        {
            ["PazaramaClientId"] = TestClientId,
            ["PazaramaClientSecret"] = TestClientSecret,
            ["BaseUrl"] = _fixture.BaseUrl
        };
    }

    private void StubTokenEndpoint()
    {
        _mockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    access_token = MockAccessToken,
                    expires_in = 3600,
                    token_type = "Bearer"
                })));
    }

    private void StubBrandEndpoint()
    {
        _mockServer
            .Given(Request.Create().WithPath("/brand/getBrands").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""data"":[],""success"":true}"));
    }

    private async Task<PazaramaAdapter> CreateConfiguredAdapterAsync()
    {
        StubTokenEndpoint();
        StubBrandEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(GetValidCredentials());
        _fixture.Reset();
        StubTokenEndpoint();

        return adapter;
    }

    // ================================================================
    // 1. TestConnectionAsync_MissingClientId_ReturnsError
    // ================================================================

    [Fact]
    public async Task TestConnectionAsync_MissingClientId_ReturnsError()
    {
        var adapter = CreateAdapter();

        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("PazaramaClientId");
        result.HttpStatusCode.Should().BeNull();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ================================================================
    // 2. PullProductsAsync_ReturnsMappedProducts
    // ================================================================

    [Fact]
    public async Task PullProductsAsync_ReturnsMappedProducts()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create().WithPath("/product/products").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                            ""name"": ""Test Urun 1"",
                            ""displayName"": ""Test Urun 1"",
                            ""code"": ""PZ-001"",
                            ""salePrice"": 49.90,
                            ""listPrice"": 59.90,
                            ""stockCount"": 25,
                            ""state"": 3,
                            ""groupCode"": ""GRP-1""
                        },
                        {
                            ""id"": ""b2c3d4e5-f6a7-8901-bcde-f12345678901"",
                            ""name"": ""Test Urun 2"",
                            ""displayName"": ""Test Urun 2"",
                            ""code"": ""PZ-002"",
                            ""salePrice"": 79.90,
                            ""listPrice"": 89.90,
                            ""stockCount"": 10,
                            ""state"": 3,
                            ""groupCode"": ""GRP-2""
                        }
                    ],
                    ""success"": true,
                    ""fromCache"": false
                }"));

        var products = await adapter.PullProductsAsync();

        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Test Urun 1");
        products[0].SKU.Should().Be("PZ-001");
        products[0].SalePrice.Should().Be(49.90m);
        products[0].Stock.Should().Be(25);
        products[0].IsActive.Should().BeTrue("state=3 means Active");
        products[1].Name.Should().Be("Test Urun 2");
        products[1].SKU.Should().Be("PZ-002");
    }

    // ================================================================
    // 3. PushStockUpdateAsync_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushStockUpdateAsync_Success_ReturnsTrue()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create().WithPath("/product/updateStock").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 42);

        result.Should().BeTrue();
    }

    // ================================================================
    // 4. PushPriceUpdateAsync_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushPriceUpdateAsync_Success_ReturnsTrue()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create().WithPath("/product/updatePrice").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 199.90m);

        result.Should().BeTrue();
    }

    // ================================================================
    // 5. PullOrdersAsync_POST_ReturnsMappedOrders
    // ================================================================

    [Fact]
    public async Task PullOrdersAsync_POST_ReturnsMappedOrders()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var orderItemId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/order/getOrdersForApi").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [{
                        ""orderId"": ""aaaabbbb-cccc-dddd-eeee-ffffffffffff"",
                        ""orderNumber"": 1001,
                        ""orderDate"": ""2026-03-01T10:00:00"",
                        ""orderAmount"": 149.90,
                        ""orderStatus"": 1,
                        ""customerId"": ""11112222-3333-4444-5555-666677778888"",
                        ""customerName"": ""Ahmet Yilmaz"",
                        ""shipmentAddress"": {
                            ""fullName"": ""Ahmet Yilmaz"",
                            ""address"": ""Ataturk Cad. No:1"",
                            ""city"": ""Istanbul"",
                            ""district"": ""Kadikoy"",
                            ""postalCode"": ""34710"",
                            ""phone"": ""05551234567""
                        },
                        ""items"": [{
                            ""orderItemId"": """ + orderItemId + @""",
                            ""orderItemStatus"": 1,
                            ""quantity"": 2,
                            ""listPrice"": 79.90,
                            ""salePrice"": 74.95,
                            ""taxAmount"": 13.49,
                            ""totalPrice"": 149.90,
                            ""deliveryType"": 1,
                            ""product"": {
                                ""productId"": ""00001111-2222-3333-4444-555566667777"",
                                ""name"": ""Test Urun"",
                                ""code"": ""PZ-001"",
                                ""vatRate"": 18
                            },
                            ""cargo"": {
                                ""companyName"": ""Yurtici Kargo"",
                                ""trackingNumber"": ""YK123456789""
                            }
                        }]
                    }],
                    ""success"": true
                }"));

        var orders = await adapter.PullOrdersAsync(since: new DateTime(2026, 3, 1));

        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be($"1001:{orderItemId}");
        orders[0].PlatformCode.Should().Be("Pazarama");
        orders[0].OrderNumber.Should().Be("1001");
        orders[0].CustomerName.Should().Be("Ahmet Yilmaz");
        orders[0].CustomerCity.Should().Be("Istanbul");
        orders[0].TotalAmount.Should().Be(149.90m);
        orders[0].CargoProviderName.Should().Be("Yurtici Kargo");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("PZ-001");
        orders[0].Lines[0].Quantity.Should().Be(2);

        var orderRequests = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/order/getOrdersForApi").ToList();
        orderRequests.Should().NotBeEmpty();
        orderRequests[0].RequestMessage.Method.Should().Be("POST");
    }

    // ================================================================
    // 6. SendShipmentAsync_TwoStage_BothSucceed
    // ================================================================

    [Fact]
    public async Task SendShipmentAsync_TwoStage_BothSucceed()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var orderItemId = Guid.NewGuid();
        var platformOrderId = $"1001:{orderItemId}";

        _mockServer
            .Given(Request.Create().WithPath("/order/updateOrderStatus").UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.SendShipmentAsync(
            platformOrderId, "YK987654321", CargoProvider.YurticiKargo);

        result.Should().BeTrue();

        var statusCalls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/order/updateOrderStatus").ToList();
        statusCalls.Should().HaveCount(2, "2-stage shipment: Hazirlaniyor + Kargoya Verildi");
        statusCalls[0].RequestMessage.Body.Should().Contain("12");
        statusCalls[1].RequestMessage.Body.Should().Contain("5");
        statusCalls[1].RequestMessage.Body.Should().Contain("YK987654321");
    }

    // ================================================================
    // 7. PullClaimsAsync_ReturnsMappedClaims
    // ================================================================

    [Fact]
    public async Task PullClaimsAsync_ReturnsMappedClaims()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var refundId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/order/getRefund").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": {
                        ""responsePage"": 1,
                        ""pageReport"": 1,
                        ""refundList"": [{
                            ""refundId"": """ + refundId + @""",
                            ""orderNumber"": 1001,
                            ""refundNumber"": ""RF-001"",
                            ""refundType"": 1,
                            ""refundStatus"": 1,
                            ""refundAmount"": 149.90,
                            ""customerName"": ""Ahmet Yilmaz"",
                            ""productName"": ""Test Urun"",
                            ""productCode"": ""PZ-001""
                        }]
                    },
                    ""success"": true
                }"));

        var claims = await adapter.PullClaimsAsync();

        claims.Should().HaveCount(1);
        claims[0].PlatformClaimId.Should().Be(refundId.ToString());
        claims[0].PlatformCode.Should().Be("Pazarama");
        claims[0].OrderNumber.Should().Be("1001");
        claims[0].Status.Should().Be("Onay Bekliyor");
        claims[0].Reason.Should().Be("Iade");
        claims[0].CustomerName.Should().Be("Ahmet Yilmaz");
        claims[0].Amount.Should().Be(149.90m);
        claims[0].Lines.Should().HaveCount(1);
        claims[0].Lines[0].SKU.Should().Be("PZ-001");
        claims[0].Lines[0].ProductName.Should().Be("Test Urun");
    }

    // ================================================================
    // 8. ApproveClaimAsync_SendsStatus2
    // ================================================================

    [Fact]
    public async Task ApproveClaimAsync_SendsStatus2()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var refundId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/order/updateRefund").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.ApproveClaimAsync(refundId.ToString());

        result.Should().BeTrue();
        var refundCalls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/order/updateRefund").ToList();
        refundCalls.Should().HaveCount(1);
        refundCalls[0].RequestMessage.Body.Should().Contain("\"status\":2");
    }

    // ================================================================
    // 9. RejectClaimAsync_SendsStatus3
    // ================================================================

    [Fact]
    public async Task RejectClaimAsync_SendsStatus3()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var refundId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/order/updateRefund").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.RejectClaimAsync(refundId.ToString(), "Urun kullanilmis");

        result.Should().BeTrue();
        var refundCalls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/order/updateRefund").ToList();
        refundCalls.Should().HaveCount(1);
        refundCalls[0].RequestMessage.Body.Should().Contain("\"status\":3");
    }

    // ================================================================
    // 10. SendInvoiceLinkAsync_Success
    // ================================================================

    [Fact]
    public async Task SendInvoiceLinkAsync_Success()
    {
        var adapter = await CreateConfiguredAdapterAsync();
        var orderId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/order/invoice-link").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"":true}"));

        var result = await adapter.SendInvoiceLinkAsync(
            orderId.ToString(), "https://fatura.example.com/inv-001.pdf");

        result.Should().BeTrue();
        var invoiceCalls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/order/invoice-link").ToList();
        invoiceCalls.Should().HaveCount(1);
        invoiceCalls[0].RequestMessage.Body.Should().Contain("https://fatura.example.com/inv-001.pdf");
        invoiceCalls[0].RequestMessage.Body.Should().Contain(orderId.ToString());
    }

    // ================================================================
    // 11. EnsureConfigured_ThrowsWhenNotConfigured
    // ================================================================

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        var adapter = CreateAdapter();

        var act = () => adapter.PullProductsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*konfigure*");
    }

    // ================================================================
    // 12. TestConnectionAsync_ValidCredentials_ReturnsSuccess
    // ================================================================

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        StubTokenEndpoint();
        StubBrandEndpoint();

        var adapter = CreateAdapter();

        var result = await adapter.TestConnectionAsync(GetValidCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Pazarama");
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);

        // Token endpoint POST /connect/token cagrildi mi?
        _mockServer.LogEntries.Should().Contain(e =>
            e.RequestMessage.Path == "/connect/token" &&
            e.RequestMessage.Method == "POST");
    }

    // ================================================================
    // 13. Token_InvalidCredentials_ReturnsConnectionError
    // ================================================================

    [Fact]
    public async Task Token_InvalidCredentials_ReturnsConnectionError()
    {
        // Token endpoint 401 — invalid credentials
        _mockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""invalid_client"",""error_description"":""Invalid credentials""}"));

        var adapter = CreateAdapter();

        var result = await adapter.TestConnectionAsync(GetValidCredentials());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ================================================================
    // 14. Token_ServerError_ReturnsConnectionError
    // ================================================================

    [Fact]
    public async Task Token_ServerError_ReturnsConnectionError()
    {
        // Token endpoint 500
        _mockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateAdapter();

        var result = await adapter.TestConnectionAsync(GetValidCredentials());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ================================================================
    // 15. Token_MissingSecret_ReturnsValidationError
    // ================================================================

    [Fact]
    public async Task Token_MissingSecret_ReturnsValidationError()
    {
        var adapter = CreateAdapter();
        var creds = new Dictionary<string, string>
        {
            ["PazaramaClientId"] = TestClientId
            // PazaramaClientSecret eksik
        };

        var result = await adapter.TestConnectionAsync(creds);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("PazaramaClientSecret");
        _mockServer.LogEntries.Should().BeEmpty("no HTTP call should be made");
    }

    // ================================================================
    // 16. PushProductAsync_BatchCreate_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushProductAsync_BatchCreate_ReturnsTrue()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        var batchId = Guid.NewGuid();

        // POST /product/create → returns batchRequestId
        _mockServer
            .Given(Request.Create().WithPath("/product/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""data"":{{""batchRequestId"":""{batchId}""}},""success"":true}}"));

        // GET /product/getProductBatchResult → Done (status=2)
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/getProductBatchResult")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""data"": {{
                        ""batchRequestId"": ""{batchId}"",
                        ""status"": 2,
                        ""totalCount"": 1,
                        ""successCount"": 1,
                        ""failedCount"": 0,
                        ""batchResult"": []
                    }},
                    ""success"": true
                }}"));

        var product = new Product
        {
            Name = "Yeni Pazarama Urun",
            SKU = "PAZ-NEW-001",
            Barcode = "8690009999999",
            SalePrice = 199.90m,
            Stock = 50
        };

        var result = await adapter.PushProductAsync(product);

        result.Should().BeTrue();

        // POST /product/create cagrildi mi?
        _mockServer.LogEntries.Should().Contain(e =>
            e.RequestMessage.Path == "/product/create" &&
            e.RequestMessage.Method == "POST");
    }

    // ================================================================
    // 17. PushProductAsync_BatchError_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task PushProductAsync_BatchError_ReturnsFalse()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        var batchId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/product/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""data"":{{""batchRequestId"":""{batchId}""}},""success"":true}}"));

        // Batch result → Error (status=3)
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/getProductBatchResult")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""data"": {{
                        ""batchRequestId"": ""{batchId}"",
                        ""status"": 3,
                        ""totalCount"": 1,
                        ""successCount"": 0,
                        ""failedCount"": 1,
                        ""batchResult"": [
                            {{""stockCode"":""PAZ-ERR-1"",""status"":""Error"",""message"":""Invalid category""}}
                        ]
                    }},
                    ""success"": true
                }}"));

        var product = new Product
        {
            Name = "Hata Urun",
            SKU = "PAZ-ERR-001",
            SalePrice = 50.00m,
            Stock = 5
        };

        var result = await adapter.PushProductAsync(product);

        result.Should().BeFalse();
    }

    // ================================================================
    // 18. PushProductAsync_BatchDoneWithFailures_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task PushProductAsync_BatchDoneWithFailures_ReturnsFalse()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        var batchId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create().WithPath("/product/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""data"":{{""batchRequestId"":""{batchId}""}},""success"":true}}"));

        // Done (status=2) but failedCount > 0
        _mockServer
            .Given(Request.Create()
                .WithPath("/product/getProductBatchResult")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""data"": {{
                        ""batchRequestId"": ""{batchId}"",
                        ""status"": 2,
                        ""totalCount"": 3,
                        ""successCount"": 1,
                        ""failedCount"": 2,
                        ""batchResult"": [
                            {{""stockCode"":""PAZ-ERR-1"",""status"":""Error"",""message"":""Barcode exists""}}
                        ]
                    }},
                    ""success"": true
                }}"));

        var product = new Product
        {
            Name = "Partial Fail",
            SKU = "PAZ-PARTIAL",
            SalePrice = 75.00m,
            Stock = 10
        };

        var result = await adapter.PushProductAsync(product);

        result.Should().BeFalse("batch completed but has failures");
    }

    // ================================================================
    // 19. GetCategoriesAsync_ReturnsLeafCategories
    // ================================================================

    [Fact]
    public async Task GetCategoriesAsync_ReturnsLeafCategories()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create().WithPath("/category/getCategoryTree").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""id"": ""11111111-1111-1111-1111-111111111111"",
                            ""displayName"": ""Elektronik"",
                            ""leaf"": false,
                            ""parentCategories"": [
                                {
                                    ""id"": ""22222222-2222-2222-2222-222222222222"",
                                    ""displayName"": ""Telefon"",
                                    ""leaf"": true,
                                    ""parentCategories"": []
                                }
                            ]
                        },
                        {
                            ""id"": ""33333333-3333-3333-3333-333333333333"",
                            ""displayName"": ""Giyim"",
                            ""leaf"": true,
                            ""parentCategories"": []
                        }
                    ],
                    ""success"": true
                }"));

        var categories = await adapter.GetCategoriesAsync();

        categories.Should().NotBeEmpty();
        // Sadece leaf kategoriler donmeli
        categories.Should().Contain(c => c.Name == "Telefon");
        categories.Should().Contain(c => c.Name == "Giyim");
    }

    // ================================================================
    // 20. GetCategoriesAsync_EmptyTree_ReturnsEmptyList
    // ================================================================

    [Fact]
    public async Task GetCategoriesAsync_EmptyTree_ReturnsEmptyList()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create().WithPath("/category/getCategoryTree").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""data"":[],""success"":true}"));

        var categories = await adapter.GetCategoriesAsync();

        categories.Should().BeEmpty();
    }

    // ================================================================
    // 21. AnyEndpoint_Unauthorized_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task AnyEndpoint_Unauthorized_ReturnsFalse()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        // Product endpoint 401
        _mockServer
            .Given(Request.Create().WithPath("/product/products").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""message"":""Unauthorized""}"));

        var products = await adapter.PullProductsAsync();

        // Adapter bos liste doner (exception degil)
        products.Should().BeEmpty();
    }

    // ================================================================
    // 22. AnyEndpoint_ServerError_ReturnsEmptyOrFalse
    // ================================================================

    [Fact]
    public async Task AnyEndpoint_ServerError_ReturnsEmptyOrFalse()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        // Stock update endpoint 500
        _mockServer
            .Given(Request.Create().WithPath("/product/updateStock").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Polly retry sonrasi hala 500 → false doner
        result.Should().BeFalse();
    }

    // ================================================================
    // 23. SendShipmentAsync_InvalidPackageId_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task SendShipmentAsync_InvalidPackageId_ReturnsFalse()
    {
        var adapter = await CreateConfiguredAdapterAsync();

        // Gecersiz packageId format (orderNumber:orderItemId olmali)
        var result = await adapter.SendShipmentAsync(
            "invalid-format", "YK123456789", CargoProvider.YurticiKargo);

        result.Should().BeFalse("invalid packageId format should fail gracefully");
        _mockServer.LogEntries.Should().NotContain(e =>
            e.RequestMessage.Path == "/order/updateOrderStatus",
            "no API call should be made for invalid format");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
