using System.Net.Http;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// N11Adapter WireMock contract testleri — Dalga 5.
/// SOAP-based adapter: SimpleSoapClient + N11SoapRequestBuilder.
/// 1-arg constructor (logger). Configure(appKey, appSecret, baseUrl, httpClient).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "N11")]
public class N11AdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<N11Adapter> _logger;

    private const string TestAppKey = "test-n11-app-key";
    private const string TestAppSecret = "test-n11-app-secret";

    public N11AdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<N11Adapter>();
    }

    private static Mock<IHttpClientFactory> CreateMockFactory()
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return mock;
    }

    private N11Adapter CreateConfiguredAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        var adapter = new N11Adapter(_logger, CreateMockFactory().Object);
        adapter.Configure(TestAppKey, TestAppSecret, _fixture.BaseUrl, httpClient);
        return adapter;
    }

    private Dictionary<string, string> GetValidCredentials()
    {
        return new Dictionary<string, string>
        {
            ["N11AppKey"] = TestAppKey,
            ["N11AppSecret"] = TestAppSecret,
            ["N11BaseUrl"] = _fixture.BaseUrl
        };
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    // ================================================================
    // 1. TestConnection_Success — GetTopLevelCategories returns valid response
    // ================================================================

    [Fact]
    public async Task TestConnection_Success_ReturnsIsSuccess()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/CategoryService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(3)));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.TestConnectionAsync(GetValidCredentials());

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("N11");
        result.StoreName.Should().Be("N11 Marketplace");
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    // ================================================================
    // 2. TestConnection_SoapFault_ReturnsError
    // ================================================================

    [Fact]
    public async Task TestConnection_SoapFault_ReturnsError()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/CategoryService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Authentication failed")));

        var adapter = new N11Adapter(_logger, CreateMockFactory().Object);
        var result = await adapter.TestConnectionAsync(GetValidCredentials());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Authentication failed");
    }

    // ================================================================
    // 3. PullProducts_SinglePage_ReturnsProducts — 2 products in response
    // ================================================================

    [Fact]
    public async Task PullProducts_SinglePage_ReturnsProducts()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapResponse(@"
                    <GetProductListResponse xmlns=""urn:partnerService"">
                      <result><status>success</status></result>
                      <productList>
                        <products>
                          <id>1001</id>
                          <productSellerCode>SKU-001</productSellerCode>
                          <title>Test Product 1</title>
                          <price>99.99</price>
                          <stockItems><stockItem><quantity>10</quantity></stockItem></stockItems>
                        </products>
                        <products>
                          <id>1002</id>
                          <productSellerCode>SKU-002</productSellerCode>
                          <title>Test Product 2</title>
                          <price>149.50</price>
                          <stockItems><stockItem><quantity>25</quantity></stockItem></stockItems>
                        </products>
                        <totalCount>2</totalCount>
                        <currentPage>0</currentPage>
                        <pageSize>100</pageSize>
                      </productList>
                      <pagingData>
                        <currentPage>0</currentPage>
                        <pageSize>100</pageSize>
                        <totalCount>2</totalCount>
                        <totalPage>1</totalPage>
                      </pagingData>
                    </GetProductListResponse>")));

        var adapter = CreateConfiguredAdapter();
        var products = await adapter.PullProductsAsync();

        products.Should().HaveCount(2);
        products[0].SKU.Should().Be("SKU-001");
        products[0].Name.Should().Be("Test Product 1");
        products[0].SalePrice.Should().Be(99.99m);
        products[0].Stock.Should().Be(10);
        products[1].SKU.Should().Be("SKU-002");
        products[1].Name.Should().Be("Test Product 2");
        products[1].SalePrice.Should().Be(149.50m);
        products[1].Stock.Should().Be(25);
    }

    // ================================================================
    // 4. PullProducts_MultiPage_PaginatesCorrectly
    // ================================================================

    [Fact]
    public async Task PullProducts_MultiPage_PaginatesCorrectly()
    {
        // Page 0 response (totalPage=2)
        var page0Body = SoapWireMockHelper.BuildSoapResponse(@"
            <GetProductListResponse xmlns=""urn:partnerService"">
              <result><status>success</status></result>
              <productList>
                <products>
                  <id>1001</id>
                  <productSellerCode>SKU-P0</productSellerCode>
                  <title>Page 0 Product</title>
                  <stockItems><stockItem><quantity>5</quantity></stockItem></stockItems>
                </products>
                <totalCount>2</totalCount>
                <currentPage>0</currentPage>
                <pageSize>1</pageSize>
              </productList>
              <pagingData>
                <currentPage>0</currentPage>
                <pageSize>1</pageSize>
                <totalCount>2</totalCount>
                <totalPage>2</totalPage>
              </pagingData>
            </GetProductListResponse>");

        // Page 1 response (totalPage=2)
        var page1Body = SoapWireMockHelper.BuildSoapResponse(@"
            <GetProductListResponse xmlns=""urn:partnerService"">
              <result><status>success</status></result>
              <productList>
                <products>
                  <id>1002</id>
                  <productSellerCode>SKU-P1</productSellerCode>
                  <title>Page 1 Product</title>
                  <stockItems><stockItem><quantity>8</quantity></stockItem></stockItems>
                </products>
                <totalCount>2</totalCount>
                <currentPage>1</currentPage>
                <pageSize>1</pageSize>
              </productList>
              <pagingData>
                <currentPage>1</currentPage>
                <pageSize>1</pageSize>
                <totalCount>2</totalCount>
                <totalPage>2</totalPage>
              </pagingData>
            </GetProductListResponse>");

        // WireMock: use AtPriority to serve different pages
        // First call returns page 0, second call returns page 1
        // We use a scenario to handle sequential responses
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .InScenario("pagination")
            .WillSetStateTo("page1")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(page0Body));

        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .InScenario("pagination")
            .WhenStateIs("page1")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(page1Body));

        var adapter = CreateConfiguredAdapter();
        var products = await adapter.PullProductsAsync();

        products.Should().HaveCount(2);
        products[0].SKU.Should().Be("SKU-P0");
        products[1].SKU.Should().Be("SKU-P1");

        // Verify 2 requests were made
        _mockServer.LogEntries.Should().HaveCount(2);
    }

    // ================================================================
    // 5. PullProducts_EmptyResult_ReturnsEmptyList
    // ================================================================

    [Fact]
    public async Task PullProducts_EmptyResult_ReturnsEmptyList()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapResponse(@"
                    <GetProductListResponse xmlns=""urn:partnerService"">
                      <result><status>success</status></result>
                      <productList>
                        <totalCount>0</totalCount>
                        <currentPage>0</currentPage>
                        <pageSize>100</pageSize>
                      </productList>
                      <pagingData>
                        <currentPage>0</currentPage>
                        <pageSize>100</pageSize>
                        <totalCount>0</totalCount>
                        <totalPage>1</totalPage>
                      </pagingData>
                    </GetProductListResponse>")));

        var adapter = CreateConfiguredAdapter();
        var products = await adapter.PullProductsAsync();

        products.Should().BeEmpty();
    }

    // ================================================================
    // 6. PushStockUpdate_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 42);

        result.Should().BeTrue();
    }

    // ================================================================
    // 7. PushStockUpdate_SoapFault_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task PushStockUpdate_SoapFault_ReturnsFalse()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Product not found")));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        result.Should().BeFalse();
    }

    // ================================================================
    // 8. PushPriceUpdate_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushPriceUpdate_Success_ReturnsTrue()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 199.99m);

        result.Should().BeTrue();
    }

    // ================================================================
    // 9. PushPriceUpdate_SoapFault_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task PushPriceUpdate_SoapFault_ReturnsFalse()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Invalid price format")));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);

        result.Should().BeFalse();
    }

    // ================================================================
    // 10. PullOrders_ReturnsOrders — parse ExternalOrderDto fields
    // ================================================================

    [Fact]
    public async Task PullOrders_ReturnsOrders_WithParsedFields()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/OrderService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapResponse(@"
                    <DetailedOrderListResponse xmlns=""urn:partnerService"">
                      <result><status>success</status></result>
                      <orderList>
                        <id>900001</id>
                        <orderNumber>N11-ORD-001</orderNumber>
                        <status>New</status>
                        <totalAmount>299.99</totalAmount>
                        <createDate>2026-03-09 10:00:00</createDate>
                        <buyer>
                          <fullName>Ali Veli</fullName>
                          <email>ali@example.com</email>
                          <phone>+905551234567</phone>
                        </buyer>
                        <shippingAddress>
                          <address>Ataturk Cad. No:1</address>
                          <city>Istanbul</city>
                        </shippingAddress>
                        <orderItems>
                          <orderItem>
                            <id>800001</id>
                            <sellerCode>SKU-100</sellerCode>
                            <productName>Test Urun A</productName>
                            <quantity>2</quantity>
                            <price>149.99</price>
                            <totalPrice>299.98</totalPrice>
                          </orderItem>
                        </orderItems>
                      </orderList>
                      <pagingData>
                        <totalCount>1</totalCount>
                        <totalPage>1</totalPage>
                      </pagingData>
                    </DetailedOrderListResponse>")));

        var adapter = CreateConfiguredAdapter();
        var orders = await adapter.PullOrdersAsync();

        orders.Should().HaveCount(1);

        var order = orders[0];
        order.PlatformCode.Should().Be("N11");
        order.PlatformOrderId.Should().Be("900001");
        order.OrderNumber.Should().Be("N11-ORD-001");
        order.Status.Should().Be("New");
        order.TotalAmount.Should().Be(299.99m);
        order.CustomerName.Should().Be("Ali Veli");
        order.CustomerEmail.Should().Be("ali@example.com");
        order.CustomerPhone.Should().Be("+905551234567");
        order.CustomerAddress.Should().Be("Ataturk Cad. No:1");
        order.CustomerCity.Should().Be("Istanbul");
        order.Currency.Should().Be("TRY");

        order.Lines.Should().HaveCount(1);
        order.Lines[0].PlatformLineId.Should().Be("800001");
        order.Lines[0].SKU.Should().Be("SKU-100");
        order.Lines[0].ProductName.Should().Be("Test Urun A");
        order.Lines[0].Quantity.Should().Be(2);
        order.Lines[0].UnitPrice.Should().Be(149.99m);
        order.Lines[0].LineTotal.Should().Be(299.98m);
    }

    // ================================================================
    // 11. PullOrders_WithPagination_FetchesAllPages
    // ================================================================

    [Fact]
    public async Task PullOrders_WithPagination_FetchesAllPages()
    {
        var page0 = SoapWireMockHelper.BuildSoapResponse(@"
            <DetailedOrderListResponse xmlns=""urn:partnerService"">
              <result><status>success</status></result>
              <orderList>
                <id>100001</id>
                <status>New</status>
                <createDate>2026-03-09 10:00:00</createDate>
              </orderList>
              <pagingData>
                <totalCount>2</totalCount>
                <totalPage>2</totalPage>
              </pagingData>
            </DetailedOrderListResponse>");

        var page1 = SoapWireMockHelper.BuildSoapResponse(@"
            <DetailedOrderListResponse xmlns=""urn:partnerService"">
              <result><status>success</status></result>
              <orderList>
                <id>100002</id>
                <status>Shipped</status>
                <createDate>2026-03-09 11:00:00</createDate>
              </orderList>
              <pagingData>
                <totalCount>2</totalCount>
                <totalPage>2</totalPage>
              </pagingData>
            </DetailedOrderListResponse>");

        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/OrderService.wsdl")
                .UsingPost())
            .InScenario("order-pagination")
            .WillSetStateTo("page1")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(page0));

        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/OrderService.wsdl")
                .UsingPost())
            .InScenario("order-pagination")
            .WhenStateIs("page1")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(page1));

        var adapter = CreateConfiguredAdapter();
        var orders = await adapter.PullOrdersAsync();

        orders.Should().HaveCount(2);
        orders[0].PlatformOrderId.Should().Be("100001");
        orders[1].PlatformOrderId.Should().Be("100002");

        _mockServer.LogEntries.Should().HaveCount(2);
    }

    // ================================================================
    // 12. GetCategories_ReturnsTopLevel
    // ================================================================

    [Fact]
    public async Task GetCategories_ReturnsTopLevelCategories()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/CategoryService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(5)));

        var adapter = CreateConfiguredAdapter();
        var categories = await adapter.GetCategoriesAsync();

        categories.Should().HaveCount(5);
        categories[0].PlatformCategoryId.Should().Be(1);
        categories[0].Name.Should().Be("Kategori 1");
        categories[4].PlatformCategoryId.Should().Be(5);
        categories[4].Name.Should().Be("Kategori 5");
    }

    // ================================================================
    // 13. UpdateOrderStatus_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task UpdateOrderStatus_Success_ReturnsTrue()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/OrderService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapResponse(@"
                    <OrderItemAcceptResponse xmlns=""urn:partnerService"">
                      <result><status>success</status></result>
                    </OrderItemAcceptResponse>")));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.UpdateOrderStatusAsync("12345", "Approved");

        result.Should().BeTrue();
    }

    // ================================================================
    // 14. UpdateOrderStatus_InvalidOrder_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task UpdateOrderStatus_InvalidOrder_ReturnsFalse()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/OrderService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Client", "Order item not found")));

        var adapter = CreateConfiguredAdapter();
        var result = await adapter.UpdateOrderStatusAsync("99999", "Approved");

        result.Should().BeFalse();
    }

    // ================================================================
    // 15. PushProduct_Success_ReturnsTrue
    // ================================================================

    [Fact]
    public async Task PushProduct_Success_ReturnsTrue()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11SaveProductResponse("5001")));

        var adapter = CreateConfiguredAdapter();
        var product = new Product
        {
            SKU = "TEST-SKU-001",
            Name = "Test Product",
            SalePrice = 199.99m,
            Stock = 50,
            Description = "A test product description"
        };

        var result = await adapter.PushProductAsync(product);

        result.Should().BeTrue();
    }

    // ================================================================
    // 16. TestConnection_MissingCredentials_ReturnsError
    // ================================================================

    [Fact]
    public async Task TestConnection_MissingCredentials_ReturnsError()
    {
        var adapter = new N11Adapter(_logger, CreateMockFactory().Object);
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("N11AppKey");
        result.PlatformCode.Should().Be("N11");
    }

    // ================================================================
    // 17. UpdateOrderStatus_NonNumericPackageId_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task UpdateOrderStatus_NonNumericPackageId_ReturnsFalse()
    {
        var adapter = CreateConfiguredAdapter();
        var result = await adapter.UpdateOrderStatusAsync("invalid-id", "Approved");

        result.Should().BeFalse();
        // No request should have been made
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ================================================================
    // 18. PushProduct_SoapFault_ReturnsFalse
    // ================================================================

    [Fact]
    public async Task PushProduct_SoapFault_ReturnsFalse()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/ProductService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Category is required")));

        var adapter = CreateConfiguredAdapter();
        var product = new Product
        {
            SKU = "FAIL-SKU",
            Name = "Fail Product",
            SalePrice = 10m,
            Stock = 1
        };

        var result = await adapter.PushProductAsync(product);

        result.Should().BeFalse();
    }
}
