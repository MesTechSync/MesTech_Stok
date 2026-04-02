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
[Trait("Category", "PttAvm")]
public class PttAvmAdapterTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _httpClient;
    private readonly PttAvmAdapter _sut;

    public PttAvmAdapterTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        SetupResponse(HttpStatusCode.OK, """{"isSuccess":true}""");

        _httpClient = new HttpClient(_handler.Object) { BaseAddress = new Uri("https://apigw.pttavm.com") };
        var options = Options.Create(new PttAvmOptions
        {
            BaseUrl = "https://apigw.pttavm.com",
            TokenEndpoint = "https://apigw.pttavm.com/auth"
        });
        _sut = new PttAvmAdapter(_httpClient, NullLogger<PttAvmAdapter>.Instance, options);

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ApiKey"] = "test", ["ApiSecret"] = "test",
            ["BaseUrl"] = "https://apigw.pttavm.com"
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
    public async Task PullProducts_Success_ReturnsProducts()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"data":{"products":[
            {"barcode":"PTT-001","productName":"PttAVM Test","stockQuantity":8,
             "salePrice":75.50,"listPrice":80.00,"categoryId":100}
        ]},"isSuccess":true}""");

        var products = await _sut.PullProductsAsync();
        products.Should().NotBeNull();
    }

    [Fact]
    public async Task PullOrders_Success_ReturnsOrders()
    {
        SetupResponse(HttpStatusCode.OK, """
        {"data":{"orders":[
            {"orderNumber":"PTT-ORD-001","status":"Onaylandi",
             "orderDate":"2026-04-01","totalAmount":150.00,
             "items":[{"sku":"PTT-SKU-1","name":"Test","quantity":1,"price":150}]}
        ]},"isSuccess":true}""");

        var orders = await _sut.PullOrdersAsync(DateTime.UtcNow.AddDays(-1));
        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task Ping_Reachable_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK, """{"isSuccess":true}""");
        var result = await _sut.PingAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdate_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, """{"isSuccess":false}""");
        var result = await _sut.PushStockUpdateAsync(Guid.NewGuid(), 10);
        result.Should().BeFalse();
    }
}
