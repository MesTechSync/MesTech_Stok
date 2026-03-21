using System.Net;
using FluentAssertions;
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
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("OpenCart");
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
        successCount.Should().Be(10);
        // 1 TestConnection request + 10 stock update requests = 11
        _handler.CapturedRequests.Count.Should().Be(11);
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
        products.Should().NotBeEmpty();
        products.Count.Should().Be(100);
    }

    [Fact]
    public void PlatformCode_IsOpenCart()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        adapter.PlatformCode.Should().Be("OpenCart");
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeFalse();
    }
}
