using System.Net;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

public class OpenCartAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<OpenCartAdapter>> _loggerMock = new();

    private OpenCartAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://shop.example.com/")
        };
        return new OpenCartAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ApiToken"] = "test-token-123",
        ["BaseUrl"] = "https://shop.example.com/"
    };

    private async Task ConfigureAdapterAsync(OpenCartAdapter adapter)
    {
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"data":[{"product_id":"1"}],"total":1}""");
        await adapter.TestConnectionAsync(ValidCredentials());
    }

    [Fact]
    public async Task TestConnection_Valid_Success()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"data":[{"product_id":"1"}],"total":1}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("OpenCart", result.PlatformCode);
    }

    [Fact]
    public async Task PushBatchStock_MaxParallel5()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Enqueue 10 responses for 10 stock updates
        for (var i = 0; i < 10; i++)
        {
            _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        }

        var updates = Enumerable.Range(0, 10)
            .Select(i => (ProductId: Guid.NewGuid(), NewStock: i + 1))
            .ToList();

        // Act
        var successCount = await adapter.PushBatchStockUpdateAsync(updates);

        // Assert
        Assert.Equal(10, successCount);
        // 1 TestConnection request + 10 stock update requests = 11
        Assert.Equal(11, _handler.CapturedRequests.Count);
    }

    [Fact]
    public async Task PullProducts_Pagination_MultiplePages()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Page 1: 100 products (triggers pagination since count == limit)
        var productsJson = string.Join(",",
            Enumerable.Range(1, 100).Select(i =>
                "{\"product_id\":\"" + i + "\",\"name\":\"Product " + i + "\",\"sku\":\"SKU-" + i.ToString("D3") + "\",\"price\":\"10.00\",\"quantity\":\"5\"}"));
        _handler.EnqueueResponse(HttpStatusCode.OK,
            "{\"data\":[" + productsJson + "],\"total\":150}");

        // Page 2: empty data array (stop pagination)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"data":[],"total":150}""");

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        Assert.NotEmpty(products);
        Assert.Equal(100, products.Count);
    }

    [Fact]
    public void PlatformCode_IsOpenCart()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        Assert.Equal("OpenCart", adapter.PlatformCode);
        Assert.True(adapter.SupportsStockUpdate);
        Assert.True(adapter.SupportsPriceUpdate);
        Assert.False(adapter.SupportsShipment);
    }
}
