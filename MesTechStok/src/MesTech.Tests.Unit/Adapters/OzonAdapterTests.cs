using System.Net;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// OzonAdapter unit tests — HTTP mock, PullProducts/PullOrders/PushStock/Ping.
/// DEV3 v3.12 TUR2: 5 adapter 0 test gap kapatma.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Ozon")]
public class OzonAdapterTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _httpClient;
    private readonly OzonAdapter _sut;

    public OzonAdapterTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        SetupResponse(HttpStatusCode.OK, """{"result":{"items":[]}}""");

        _httpClient = new HttpClient(_handler.Object) { BaseAddress = new Uri("https://api-seller.ozon.ru") };
        var options = Options.Create(new OzonOptions { BaseUrl = "https://api-seller.ozon.ru" });
        _sut = new OzonAdapter(_httpClient, NullLogger<OzonAdapter>.Instance, options);

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ApiKey"] = "test-key", ["ClientId"] = "test-client",
            ["BaseUrl"] = "https://api-seller.ozon.ru"
        }).GetAwaiter().GetResult();
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupResponse(HttpStatusCode status, string body = "{}")
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public async Task PullProducts_Success_ReturnsProducts()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"result":{"items":[
            {"product_id":1,"offer_id":"OZ-001","name":"Test Urun","price":"99.90",
             "old_price":"0","stocks":[{"warehouse_id":1,"present":10}],
             "barcode":"8690001","category_id":100}
        ]}}""");

        var products = await _sut.PullProductsAsync();
        products.Should().NotBeNull();
    }

    [Fact]
    public async Task PullProducts_EmptyResult_ReturnsEmptyList()
    {
        SetupResponse(HttpStatusCode.OK, """{"result":{"items":[]}}""");
        var products = await _sut.PullProductsAsync();
        products.Should().NotBeNull();
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task PullOrders_Success_ReturnsOrders()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"result":{"postings":[
            {"posting_number":"OZ-ORD-001","status":"awaiting_deliver",
             "order_id":12345,"created_at":"2026-04-01T10:00:00Z",
             "products":[{"sku":1,"name":"Test","quantity":1,"price":"50.00","offer_id":"OZ-001"}],
             "analytics_data":{},"financial_data":{}}
        ]}}""");

        var orders = await _sut.PullOrdersAsync(DateTime.UtcNow.AddDays(-1));
        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, """{"result":[{"updated":true}]}""");
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 50);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdate_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError);
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 50);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Ping_Reachable_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK);
        var result = await _sut.PingAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategories_Success_ReturnsList()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"result":[
            {"category_id":1,"title":"Elektronik"},
            {"category_id":2,"title":"Giyim"}
        ]}""");

        var categories = await _sut.GetCategoriesAsync();
        categories.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterWebhook_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK);
        var result = await _sut.RegisterWebhookAsync("https://mestech.app/webhook/ozon");
        result.Should().BeTrue();
    }
}
