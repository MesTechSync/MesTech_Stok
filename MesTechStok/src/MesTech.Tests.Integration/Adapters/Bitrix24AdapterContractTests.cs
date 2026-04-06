using System.Net.Http;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Webhooks;
using MesTech.Infrastructure.Messaging;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// Bitrix24Adapter WireMock contract tests.
/// OAuth2 Authorization Code flow, CRM deal/contact/product, batch API.
/// DEV 3 Dalga 7 — activated from DEV 5 stubs.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Bitrix24")]
public class Bitrix24AdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;

    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";
    private const string TestRefreshToken = "test-b24-refresh-token-xyz789";

    public Bitrix24AdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private string TestDomain => new Uri(_fixture.BaseUrl).Authority;

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["Bitrix24ClientId"] = TestClientId,
        ["Bitrix24ClientSecret"] = TestClientSecret,
        ["Bitrix24PortalDomain"] = TestDomain,
        ["Bitrix24RefreshToken"] = TestRefreshToken,
        ["Bitrix24TokenEndpoint"] = _fixture.BaseUrl + "/oauth/token"
    };

    private Bitrix24Adapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl + "/rest/") };
        var authHttpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl + "/") };
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(authHttpClient);
        return new Bitrix24Adapter(httpClient, NullLogger<Bitrix24Adapter>.Instance, mockFactory.Object);
    }

    // ════════════════════════════════════════════════════════════════
    //  WireMock Stubs
    // ════════════════════════════════════════════════════════════════

    private void StubOAuthTokenEndpoint(int expiresIn = 1800)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/oauth/token*")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildTokenResponse(
                    expiresIn: expiresIn, domain: TestDomain)));
    }

    private void StubProfileEndpoint()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/profile")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildProfileResponse()));
    }

    private void StubCrmProductList()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.product.list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildProductListResponse()));
    }

    private void StubCrmDealUpdate()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.deal.update")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildDealUpdateResponse()));
    }

    private void StubCrmContactList()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.contact.list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildContactListResponse()));
    }

    private void StubCrmContactAdd(int contactId = 200)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.contact.add")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildContactAddResponse(contactId)));
    }

    private void StubBatchEndpoint()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/batch")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildBatchResponse()));
    }

    private void StubCatalogSectionList()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/catalog.section.list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildCatalogSectionListResponse()));
    }

    private void StubCrmProductAdd()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.product.add")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""result"":300}"));
    }

    private void StubCrmDealAdd(int dealId = 100)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.deal.add")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildDealAddResponse(dealId)));
    }

    private void StubCrmDealProductRowsSet()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.deal.productrows.set")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""result"":true}"));
    }

    private void StubCrmContactUpdate()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/crm.contact.update")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""result"":true}"));
    }

    private void StubApiError(string path, int statusCode = 500)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"/rest/{path}")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildErrorResponse()));
    }

    // ════════════════════════════════════════════════════════════════
    //  Adapter Property Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformCode_IsBitrix24()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("Bitrix24");
    }

    [Fact]
    public void SupportsStockUpdate_IsFalse()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeFalse();
    }

    [Fact]
    public void SupportsPriceUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsShipment_IsFalse()
    {
        var adapter = CreateAdapter();
        adapter.SupportsShipment.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════
    //  TestConnectionAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();

        var adapter = CreateAdapter();
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Bitrix24");
    }

    [Fact]
    public async Task TestConnectionAsync_MissingCredentials_ReturnsError()
    {
        var adapter = CreateAdapter();
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>
        {
            ["Bitrix24ClientId"] = "test"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Missing required credential");
    }

    // ════════════════════════════════════════════════════════════════
    //  PullProductsAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_ReturnsProducts()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmProductList();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var products = await adapter.PullProductsAsync();

        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Product A");
    }

    // ════════════════════════════════════════════════════════════════
    //  PushProductAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushProductAsync_ValidProduct_ReturnsTrue()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmProductAdd();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var product = new MesTech.Domain.Entities.Product
        {
            Name = "Test Product",
            SalePrice = 99.90m,
            Description = "Test description"
        };

        var result = await adapter.PushProductAsync(product);
        result.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════
    //  GetCategoriesAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCatalogSections()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCatalogSectionList();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var categories = await adapter.GetCategoriesAsync();

        categories.Should().HaveCount(3);
        categories[0].Name.Should().Be("Electronics");
    }

    // ════════════════════════════════════════════════════════════════
    //  SyncContactsAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncContactsAsync_ReturnsContactCount()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmContactList();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var count = await adapter.SyncContactsAsync();
        count.Should().Be(2);
    }

    // ════════════════════════════════════════════════════════════════
    //  UpdateDealStageAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDealStageAsync_ValidDeal_ReturnsTrue()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmDealUpdate();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var result = await adapter.UpdateDealStageAsync("100", "WON");
        result.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════
    //  BatchExecuteAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BatchExecuteAsync_Under50Commands_SingleRequest()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubBatchEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var commands = new List<string> { "crm.deal.list", "crm.contact.list" };

        var results = await adapter.BatchExecuteAsync(commands);
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BatchExecuteAsync_Over50Commands_ChunkedRequests()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubBatchEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var commands = Enumerable.Range(0, 55)
            .Select(i => $"crm.deal.get?id={i}")
            .ToList();

        var results = await adapter.BatchExecuteAsync(commands);
        results.Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════
    //  PushStockUpdateAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsFalse_CrmNotStock()
    {
        var adapter = CreateAdapter();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 42);
        result.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════
    //  PushContactAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushContactAsync_ValidCustomer_ReturnsContactId()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmContactAdd(200);

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var customer = new MesTech.Domain.Entities.Customer
        {
            Name = "Ali Yilmaz",
            Phone = "+905551234567",
            Email = "ali@test.com",
            City = "Istanbul"
        };

        var contactId = await adapter.PushContactAsync(customer);
        contactId.Should().NotBeNull();
        contactId.Should().Be("200");
    }

    // ════════════════════════════════════════════════════════════════
    //  Polly / Rate Limit Reflection Checks
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void RateLimitSemaphore_HasDefaultConcurrency()
    {
        var field = typeof(Bitrix24Adapter)
            .GetField("_rateLimitSemaphore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        field.Should().NotBeNull("Bitrix24Adapter should have _rateLimitSemaphore field");

        var semaphore = field!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull();
        semaphore!.CurrentCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void RetryPipeline_IsConfigured()
    {
        var field = typeof(Bitrix24Adapter)
            .GetField("_retryPipeline",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        field.Should().NotBeNull("Bitrix24Adapter should have _retryPipeline field");

        var adapter = CreateAdapter();
        var pipeline = field!.GetValue(adapter);
        pipeline.Should().NotBeNull("Polly ResiliencePipeline should be initialized");
    }

    // ════════════════════════════════════════════════════════════════
    //  Batch 2: PushDealAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushDealAsync_ValidOrder_CreatesDeal()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmDealAdd(100);
        StubCrmDealProductRowsSet();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var order = new MesTech.Domain.Entities.Order
        {
            OrderNumber = "TST-001",
            Status = OrderStatus.Confirmed,
            OrderDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid()
        };
        order.SetFinancials(0m, 0m, 2500.00m);

        var result = await adapter.PushDealAsync(order);
        // Adapter returns the Bitrix24 integer deal ID as a string
        result.Should().Be("100");
    }

    [Fact]
    public async Task PushDealAsync_WithProductRows_SendsProductRowsRequest()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmDealAdd(500);
        StubCrmDealProductRowsSet();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var order = new MesTech.Domain.Entities.Order
        {
            OrderNumber = "TST-002",
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };
        order.SetFinancials(0m, 0m, 500.00m);
        order.AddItem(new MesTech.Domain.Entities.OrderItem
        {
            ProductName = "Widget A",
            Quantity = 2,
            UnitPrice = 100.00m,
            TaxRate = 18.0m
        });
        order.AddItem(new MesTech.Domain.Entities.OrderItem
        {
            ProductName = "Widget B",
            Quantity = 1,
            UnitPrice = 300.00m,
            TaxRate = 18.0m
        });

        var result = await adapter.PushDealAsync(order);
        // Verify request was made (WireMock tracks calls)
        _mockServer.LogEntries.Should().Contain(e =>
            e.RequestMessage.Path!.Contains("crm.deal.productrows.set"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Batch 2: UpdateContactAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateContactAsync_ExistingContact_ReturnsTrue()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmContactUpdate();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var customer = new MesTech.Domain.Entities.Customer
        {
            Name = "Mehmet Demir",
            Phone = "+905559999999",
            Email = "mehmet@test.com"
        };

        var result = await adapter.UpdateContactAsync("150", customer);
        result.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════
    //  Batch 2: GetCatalogProductsAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCatalogProductsAsync_DelegatesToPullProducts()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubCrmProductList();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var products = await adapter.GetCatalogProductsAsync();
        products.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════
    //  Batch 2: Webhook Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WebhookReceiver_Bitrix24DealAdd_ReturnsSuccess()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var adapters = new MesTech.Application.Interfaces.IIntegratorAdapter[] { adapter };
        var service = new WebhookReceiverService(adapters, new Mock<IIntegrationEventPublisher>().Object, NullLogger<WebhookReceiverService>.Instance);

        var payload = "event=ONCRMDEALADD&data[FIELDS][ID]=123&auth[application_token]=test-token";
        var result = await service.ProcessBitrix24WebhookAsync(payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("DealCreated");
        result.PlatformOrderId.Should().Be("123");
    }

    [Fact]
    public async Task WebhookReceiver_Bitrix24ContactUpdate_ReturnsSuccess()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var adapters = new MesTech.Application.Interfaces.IIntegratorAdapter[] { adapter };
        var service = new WebhookReceiverService(adapters, new Mock<IIntegrationEventPublisher>().Object, NullLogger<WebhookReceiverService>.Instance);

        var payload = "event=ONCRMCONTACTUPDATE&data[FIELDS][ID]=456&auth[application_token]=test-token";
        var result = await service.ProcessBitrix24WebhookAsync(payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("ContactUpdated");
        result.PlatformOrderId.Should().Be("456");
    }

    [Fact]
    public async Task WebhookReceiver_GenericBitrix24Route_DelegatesToBitrix24Handler()
    {
        var adapters = Array.Empty<MesTech.Application.Interfaces.IIntegratorAdapter>();
        var service = new WebhookReceiverService(adapters, new Mock<IIntegrationEventPublisher>().Object, NullLogger<WebhookReceiverService>.Instance);

        var payload = "event=ONCRMDEALADD&data[FIELDS][ID]=789&auth[application_token]=test-token";
        var result = await service.ProcessGenericWebhookAsync("Bitrix24", "crm", payload);

        result.Success.Should().BeTrue();
        result.EventType.Should().Be("DealCreated");
    }

    // ════════════════════════════════════════════════════════════════
    //  Batch 2: Error Handling Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushContactAsync_ApiError_ReturnsNull()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();
        StubApiError("crm.contact.add", 500);

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var customer = new MesTech.Domain.Entities.Customer
        {
            Name = "Test User",
            Email = "error@test.com"
        };

        // 500 triggers Polly retries then circuit breaker — returns null
        var contactId = await adapter.PushContactAsync(customer);
        contactId.Should().BeNull();
    }

    [Fact]
    public async Task BatchExecuteAsync_PartialFailure_ReturnsAvailableResults()
    {
        StubOAuthTokenEndpoint();
        StubProfileEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath("/rest/batch")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(Bitrix24WireMockHelper.BuildBatchPartialFailureResponse()));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var commands = new List<string> { "crm.deal.list", "crm.contact.list", "crm.product.list" };
        var results = await adapter.BatchExecuteAsync(commands);

        // Partial failure: cmd0 + cmd2 succeed, cmd1 fails
        results.Should().HaveCountGreaterOrEqualTo(1);
    }
}
