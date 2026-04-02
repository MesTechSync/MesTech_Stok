using System.Net;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

[Trait("Category", "Unit")]
[Trait("Category", "Zalando")]
public class ZalandoAdapterTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _httpClient;
    private readonly ZalandoAdapter _sut;

    public ZalandoAdapterTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        SetupResponse(HttpStatusCode.OK, "{}");

        _httpClient = new HttpClient(_handler.Object) { BaseAddress = new Uri("https://api.zalando.com") };
        var options = Options.Create(new ZalandoOptions { ApiBaseUrl = "https://api.zalando.com" });
        _sut = new ZalandoAdapter(_httpClient, NullLogger<ZalandoAdapter>.Instance, options);

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ClientId"] = "test", ["ClientSecret"] = "test",
            ["BaseUrl"] = "https://api.zalando.com"
        }).GetAwaiter().GetResult();
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupResponse(HttpStatusCode status, string body)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }

    [Fact]
    public async Task PullOrders_Success_ReturnsOrders()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"content":[{"order_number":"ZAL-001","status":"APPROVED",
         "order_date":"2026-04-01","gross_total":{"amount":120.00,"currency":"EUR"},
         "items":[{"sku":"Z-SKU-1","name":"Test","quantity":1,"price":{"amount":120}}]}]}""");

        var orders = await _sut.PullOrdersAsync(DateTime.UtcNow.AddDays(-1));
        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task PullProducts_Success_ReturnsProducts()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"items":[{"ean":"4001234567890","name":"Zalando Test","price":{"amount":59.99,"currency":"EUR"},
         "stock":{"quantity":25},"brand":{"name":"TestBrand"}}]}""");

        var products = await _sut.PullProductsAsync();
        products.Should().NotBeNull();
    }

    [Fact]
    public async Task Ping_Reachable_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");
        var result = await _sut.PingAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, "{}");
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 30);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdate_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, """{"error":"fail"}""");
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 30);
        result.Should().BeFalse();
    }
}
