using System.Net;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

public class TrendyolAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<TrendyolAdapter>> _loggerMock = new();

    private TrendyolAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.trendyol.com/")
        };
        return new TrendyolAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ApiKey"] = "test-api-key",
        ["ApiSecret"] = "test-api-secret",
        ["SupplierId"] = "12345"
    };

    [Fact]
    public async Task TestConnection_ValidCreds_Success()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 42}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Trendyol", result.PlatformCode);
        Assert.Equal(42, result.ProductCount);

        var requestUrl = _handler.CapturedRequests[0].RequestUri!.ToString();
        Assert.Contains("/sapigw/suppliers/12345/products", requestUrl);
    }

    [Fact]
    public async Task TestConnection_MissingSupplierId_Failure()
    {
        // Arrange — creds without SupplierId key
        var creds = new Dictionary<string, string>
        {
            ["ApiKey"] = "test-api-key",
            ["ApiSecret"] = "test-api-secret"
        };

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task PullProducts_DeserializesCorrectly()
    {
        // Arrange — first enqueue TestConnection response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");

        // Enqueue PullProducts response (single page)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"barcode":"123","title":"Test Product","stockCode":"SKU-001","salePrice":99.90,"quantity":10,"description":"Desc"}],"totalElements":1,"totalPages":1}""");

        var adapter = CreateAdapter();

        // Configure adapter via TestConnection
        await adapter.TestConnectionAsync(ValidCredentials());

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        Assert.NotEmpty(products);
        Assert.Equal("Test Product", products[0].Name);
        Assert.Equal("SKU-001", products[0].SKU);
        Assert.Equal(99.90m, products[0].SalePrice);
        Assert.Equal(10, products[0].Stock);
        Assert.Equal("Desc", products[0].Description);
    }

    [Fact]
    public async Task PushStockUpdate_CorrectPayload()
    {
        // Arrange — enqueue TestConnection response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");

        // Enqueue PushStockUpdate response
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials());

        var productId = Guid.NewGuid();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 25);

        // Assert
        Assert.True(result);
        Assert.Equal(2, _handler.CapturedRequests.Count);
    }

    [Fact]
    public void PlatformCode_IsTrendyol()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        Assert.Equal("Trendyol", adapter.PlatformCode);
        Assert.True(adapter.SupportsStockUpdate);
        Assert.True(adapter.SupportsPriceUpdate);
        Assert.True(adapter.SupportsShipment);
    }
}
