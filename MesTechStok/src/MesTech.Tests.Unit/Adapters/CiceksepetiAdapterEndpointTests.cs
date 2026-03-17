using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV 5 — CiceksepetiAdapter new endpoint unit tests.
/// 2 tests per method (happy path + error path).
/// Uses Moq HttpMessageHandler for mocking HTTP calls.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Ciceksepeti")]
public class CiceksepetiAdapterEndpointTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CiceksepetiAdapter> _logger;
    private readonly CiceksepetiAdapter _sut;
    private readonly JsonSerializerOptions _jsonOptions;

    public CiceksepetiAdapterEndpointTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://apis.ciceksepeti.com/")
        };
        _logger = NullLogger<CiceksepetiAdapter>.Instance;
        _sut = new CiceksepetiAdapter(_httpClient, _logger);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure the adapter via TestConnectionAsync mock
        ConfigureAdapter();
    }

    private void ConfigureAdapter()
    {
        SetupMockResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new { products = Array.Empty<object>(), totalCount = 0 }, _jsonOptions));

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ApiKey"] = "test-api-key"
        }).GetAwaiter().GetResult();
    }

    private void SetupMockResponse(HttpStatusCode statusCode, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            }));
    }

    #region UpdateProductAsync

    [Fact]
    public async Task UpdateProductAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var product = new CsProductUpdateDto(12345, "Test Product", 99.90m, 50, "Description", "8680001234567");

        var result = await _sut.UpdateProductAsync(product);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProductAsync_BadRequest_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid product data\"}");

        var product = new CsProductUpdateDto(0, "", -1m, -1, null, null);

        var result = await _sut.UpdateProductAsync(product);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_NullProduct_ThrowsArgumentNullException()
    {
        var act = () => _sut.UpdateProductAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteProductAsync

    [Fact]
    public async Task DeleteProductAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.DeleteProductAsync("12345");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_NotFound_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"product not found\"}");

        var result = await _sut.DeleteProductAsync("INVALID-ID");

        result.Should().BeFalse();
    }

    #endregion

    #region GetCsCategoriesAsync

    [Fact]
    public async Task GetCsCategoriesAsync_Success_ReturnsCategories()
    {
        var categories = new CsCategoryListResponse
        {
            Categories = new List<CsCategoryDto>
            {
                new(1, "Cicekler", null),
                new(2, "Guller", 1),
                new(3, "Hediyeler", null)
            }
        };
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(categories, _jsonOptions));

        var result = await _sut.GetCsCategoriesAsync();

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Cicekler");
        result[1].ParentId.Should().Be(1);
        result[2].ParentId.Should().BeNull();
    }

    [Fact]
    public async Task GetCsCategoriesAsync_ServerError_ReturnsEmptyList()
    {
        SetupMockResponse(HttpStatusCode.InternalServerError, "{\"error\":\"internal\"}");

        var result = await _sut.GetCsCategoriesAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region GetCategoryAttributesAsync

    [Fact]
    public async Task GetCategoryAttributesAsync_Success_ReturnsAttributes()
    {
        var attributes = new CsAttributeListResponse
        {
            Attributes = new List<CsAttributeDto>
            {
                new(101, "Renk", true, "select", new List<string> { "Kirmizi", "Beyaz", "Pembe" }),
                new(102, "Boy", false, "text", null)
            }
        };
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(attributes, _jsonOptions));

        var result = await _sut.GetCategoryAttributesAsync("1");

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Renk");
        result[0].Required.Should().BeTrue();
        result[0].AllowedValues.Should().Contain("Kirmizi");
        result[1].Required.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategoryAttributesAsync_NotFound_ReturnsEmptyList()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"category not found\"}");

        var result = await _sut.GetCategoryAttributesAsync("INVALID");

        result.Should().BeEmpty();
    }

    #endregion

    #region BatchUpdateStockAsync

    [Fact]
    public async Task BatchUpdateStockAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var items = new List<CsStockUpdate>
        {
            new("SKU-001", 100),
            new("SKU-002", 50),
            new("SKU-003", 200)
        };

        var result = await _sut.BatchUpdateStockAsync(items);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task BatchUpdateStockAsync_BadRequest_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid stock data\"}");

        var items = new List<CsStockUpdate> { new("INVALID", -1) };

        var result = await _sut.BatchUpdateStockAsync(items);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BatchUpdateStockAsync_NullItems_ThrowsArgumentNullException()
    {
        var act = () => _sut.BatchUpdateStockAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region BatchUpdatePriceAsync

    [Fact]
    public async Task BatchUpdatePriceAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var items = new List<CsPriceUpdate>
        {
            new("SKU-001", 99.90m),
            new("SKU-002", 149.50m)
        };

        var result = await _sut.BatchUpdatePriceAsync(items);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task BatchUpdatePriceAsync_ServerError_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.InternalServerError, "{\"error\":\"server error\"}");

        var items = new List<CsPriceUpdate> { new("SKU-001", 99.90m) };

        var result = await _sut.BatchUpdatePriceAsync(items);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BatchUpdatePriceAsync_NullItems_ThrowsArgumentNullException()
    {
        var act = () => _sut.BatchUpdatePriceAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetCargoTrackingAsync

    [Fact]
    public async Task GetCargoTrackingAsync_Success_ReturnsTracking()
    {
        var tracking = new CsTrackingDto("ORD-001", "Yurtici Kargo", "TRK-789", "InTransit",
            new DateTime(2026, 3, 15, 14, 30, 0));
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(tracking, _jsonOptions));

        var result = await _sut.GetCargoTrackingAsync("ORD-001");

        result.Should().NotBeNull();
        result!.OrderId.Should().Be("ORD-001");
        result.CargoCompany.Should().Be("Yurtici Kargo");
        result.TrackingNumber.Should().Be("TRK-789");
        result.Status.Should().Be("InTransit");
    }

    [Fact]
    public async Task GetCargoTrackingAsync_NotFound_ReturnsNull()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"order not found\"}");

        var result = await _sut.GetCargoTrackingAsync("ORD-INVALID");

        result.Should().BeNull();
    }

    #endregion

    #region EnsureConfigured Guards

    [Fact]
    public async Task UpdateProductAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var unconfigured = new CiceksepetiAdapter(
            new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://apis.ciceksepeti.com/") },
            _logger);

        var act = () => unconfigured.UpdateProductAsync(
            new CsProductUpdateDto(1, "Test", 10m, 1, null, null));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BatchUpdateStockAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var unconfigured = new CiceksepetiAdapter(
            new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://apis.ciceksepeti.com/") },
            _logger);

        var act = () => unconfigured.BatchUpdateStockAsync(new List<CsStockUpdate> { new("SKU", 1) });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetCargoTrackingAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var unconfigured = new CiceksepetiAdapter(
            new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://apis.ciceksepeti.com/") },
            _logger);

        var act = () => unconfigured.GetCargoTrackingAsync("ORD-001");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
