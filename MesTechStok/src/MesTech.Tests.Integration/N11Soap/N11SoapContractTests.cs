using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.N11Soap;

/// <summary>
/// N11 SOAP adapter contract testleri — TDD RED.
/// N11Adapter iskelet olduğu için tüm testler SKIP.
/// DEV3 H25'te N11Adapter implement edilince SKIP kaldırılacak.
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
        _adapter = new N11Adapter(new Mock<ILogger<N11Adapter>>().Object);
    }

    public void Dispose() => _server.Stop();

    // ─────────────────────────────────────────────────
    // TDD RED testleri: N11Adapter iskelet — DEV3 H25'te implement edilecek
    // ─────────────────────────────────────────────────

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task GetProductList_WithProducts_ReturnsNonEmptyList()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetProductListResponse(3)));

        var result = await _adapter.PullProductsAsync();

        result.Should().HaveCount(3);
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task GetProductList_EmptyStore_ReturnsEmpty()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetProductListResponse(0)));

        var result = await _adapter.PullProductsAsync();

        result.Should().BeEmpty();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task PushProduct_ValidProduct_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11SaveProductResponse("123456")));

        var product = N11TestData.BuildProduct("TEST-001", "Test Ürün", 100m, 10);
        var result = await _adapter.PushProductAsync(product);

        result.Should().BeTrue();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task PushProduct_SoapFault_ReturnsFalse()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Server", "Kimlik dogrulanamadi")));

        var product = N11TestData.BuildProduct("TEST-002", "Test Ürün", 100m, 5);
        var result = await _adapter.PushProductAsync(product);

        // N11Adapter SOAP Fault'u exception değil false olarak dönmeli
        result.Should().BeFalse();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task PushStockUpdate_ValidUpdate_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductStockService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var result = await _adapter.PushStockUpdateAsync(Guid.NewGuid(), 25);

        result.Should().BeTrue();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task PushPriceUpdate_ValidUpdate_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductPriceService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11UpdateProductResponse(true)));

        var result = await _adapter.PushPriceUpdateAsync(Guid.NewGuid(), 299.99m);

        result.Should().BeTrue();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task TestConnection_ValidCredentials_ReturnsSuccess()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetProductListResponse(0)));

        var credentials = new Dictionary<string, string>
        {
            ["appKey"] = "test-app-key",
            ["appSecret"] = "test-app-secret",
            ["baseUrl"] = _baseUrl
        };

        var result = await _adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("N11");
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task TestConnection_InvalidCredentials_ReturnsError()
    {
        _server.Given(Request.Create().WithPath("/ws/ProductService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildSoapFault("Client.AuthenticationException", "Invalid credentials")));

        var credentials = new Dictionary<string, string>
        {
            ["appKey"] = "wrong-key",
            ["appSecret"] = "wrong-secret",
            ["baseUrl"] = _baseUrl
        };

        var result = await _adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task GetCategories_ReturnsCategoryList()
    {
        _server.Given(Request.Create().WithPath("/ws/CategoryService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(5)));

        var result = await _adapter.GetCategoriesAsync();

        result.Should().HaveCount(5);
    }

    [Fact(Skip = "N11Adapter iskelet — DEV3 H25'te implement edilecek")]
    public async Task GetOrderList_ReturnsOrders()
    {
        // N11 siparişleri için IOrderCapableAdapter implement edilince aktif olacak
        _server.Given(Request.Create().WithPath("/ws/OrderService").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(SoapWireMockHelper.BuildN11GetOrderListResponse(2)));

        // TODO: N11Adapter IOrderCapableAdapter implemente ettiğinde
        // var orders = await _adapter.GetOrdersAsync(DateTime.UtcNow.AddDays(-1), CancellationToken.None);
        // orders.Should().HaveCount(2);
        await Task.CompletedTask; // placeholder
    }
}

/// <summary>
/// N11 kontrakt testleri için test data factory.
/// </summary>
file static class N11TestData
{
    public static MesTech.Domain.Entities.Product BuildProduct(
        string sku, string name, decimal price, int stock)
    {
        return new MesTech.Domain.Entities.Product
        {
            Barcode = sku,
            Name = name,
            SalePrice = price,
            StockQuantity = stock,
            IsActive = true
        };
    }
}
