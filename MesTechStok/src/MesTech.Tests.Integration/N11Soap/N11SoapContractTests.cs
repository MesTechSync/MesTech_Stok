using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using System.Net.Http;

namespace MesTech.Tests.Integration.N11Soap;

/// <summary>
/// N11 SOAP adapter contract testleri — D-16 SKIP->GREEN.
/// N11Adapter WireMock ile test: ProductService.wsdl, OrderService.wsdl, CategoryService.wsdl.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "N11Soap")]
[Trait("Phase", "Dalga5")]
public class N11SoapContractTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly N11Adapter _adapter;
    private readonly string _baseUrl;

    public N11SoapContractTests()
    {
        _server = WireMockServer.Start();
        _baseUrl = _server.Url!;
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        _adapter = new N11Adapter(new Mock<ILogger<N11Adapter>>().Object, mockFactory.Object);
        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        _adapter.Configure("test-app-key", "test-app-secret", _baseUrl, httpClient);
    }

    public void Dispose() => _server.Stop();

    // ════ 1. GetProductList — 3 products ════

    [Fact]
    public async Task GetProductList_WithProducts_ReturnsNonEmptyList()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetProductListResponse(3)));

        var result = await _adapter.PullProductsAsync();

        result.Should().HaveCount(3);
    }

    // ════ 2. GetProductList — empty store ════

    [Fact]
    public async Task GetProductList_EmptyStore_ReturnsEmpty()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetProductListResponse(0)));

        var result = await _adapter.PullProductsAsync();

        result.Should().BeEmpty();
    }

    // ════ 3. PushProduct — success ════

    [Fact]
    public async Task PushProduct_ValidProduct_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11SaveProductResponse("123456")));

        var product = N11TestData.BuildProduct("TEST-001", "Test Urun", 100m, 10);
        var result = await _adapter.PushProductAsync(product);

        result.Should().BeTrue();
    }

    // ════ 4. PushProduct — SOAP Fault returns false ════

    [Fact]
    public async Task PushProduct_SoapFault_ReturnsFalse()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Kimlik dogrulanamadi")));

        var product = N11TestData.BuildProduct("TEST-002", "Test Urun", 100m, 5);
        var result = await _adapter.PushProductAsync(product);

        result.Should().BeFalse();
    }

    // ════ 5. PushStockUpdate — success ════

    [Fact]
    public async Task PushStockUpdate_ValidUpdate_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var result = await _adapter.PushStockUpdateAsync(Guid.NewGuid(), 25);

        result.Should().BeTrue();
    }

    // ════ 6. PushPriceUpdate — success ════

    [Fact]
    public async Task PushPriceUpdate_ValidUpdate_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var result = await _adapter.PushPriceUpdateAsync(Guid.NewGuid(), 299.99m);

        result.Should().BeTrue();
    }

    // ════ 7. TestConnection — valid credentials ════

    [Fact]
    public async Task TestConnection_ValidCredentials_ReturnsSuccess()
    {
        _server.Given(Request.Create().WithPath("/ws/CategoryService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(5)));

        var credentials = new Dictionary<string, string>
        {
            ["N11AppKey"] = "test-app-key",
            ["N11AppSecret"] = "test-app-secret",
            ["N11BaseUrl"] = _baseUrl
        };

        var result = await _adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("N11");
    }

    // ════ 8. TestConnection — SOAP fault returns error ════

    [Fact]
    public async Task TestConnection_InvalidCredentials_ReturnsError()
    {
        _server.Given(Request.Create().WithPath("/ws/CategoryService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Client.AuthenticationException", "Invalid credentials")));

        var credentials = new Dictionary<string, string>
        {
            ["N11AppKey"] = "wrong-key",
            ["N11AppSecret"] = "wrong-secret",
            ["N11BaseUrl"] = _baseUrl
        };

        var result = await _adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════ 9. GetCategories — returns list ════

    [Fact]
    public async Task GetCategories_ReturnsCategoryList()
    {
        _server.Given(Request.Create().WithPath("/ws/CategoryService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(5)));

        var result = await _adapter.GetCategoriesAsync();

        result.Should().HaveCount(5);
    }

    // ════ 10. GetOrderList — returns orders ════

    [Fact]
    public async Task GetOrderList_ReturnsOrders()
    {
        _server.Given(Request.Create().WithPath("/ws/OrderService.wsdl").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetOrderListResponse(2)));

        var orders = await _adapter.PullOrdersAsync();

        orders.Should().HaveCount(2);
    }
}

/// <summary>
/// N11 kontrakt testleri icin test data factory.
/// </summary>
file static class N11TestData
{
    public static MesTech.Domain.Entities.Product BuildProduct(
        string sku, string name, decimal price, int stock)
    {
        return new MesTech.Domain.Entities.Product
        {
            SKU = sku,
            Name = name,
            SalePrice = price,
            Stock = stock,
            IsActive = true
        };
    }
}
