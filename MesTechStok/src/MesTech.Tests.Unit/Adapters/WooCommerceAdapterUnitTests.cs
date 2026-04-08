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
[Trait("Platform", "WooCommerce")]
public class WooCommerceAdapterUnitTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _httpClient;
    private readonly WooCommerceAdapter _sut;

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["SiteUrl"] = "https://mystore.example.com",
        ["ConsumerKey"] = "ck_test_key_123",
        ["ConsumerSecret"] = "cs_test_secret_456"
    };

    private static readonly Dictionary<string, string> InvalidCredentials = new()
    {
        ["SiteUrl"] = "",
        ["ConsumerKey"] = "",
        ["ConsumerSecret"] = ""
    };

    public WooCommerceAdapterUnitTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_handler.Object)
        {
            BaseAddress = new Uri("https://mystore.example.com")
        };

        var options = Options.Create(new WooCommerceOptions());
        _sut = new WooCommerceAdapter(_httpClient, NullLogger<WooCommerceAdapter>.Instance, options);
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupResponse(HttpStatusCode status, string body)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    // ─── 1. PlatformCode ───────────────────────────────────────

    [Fact]
    public void PlatformCode_ShouldBeWooCommerce()
    {
        _sut.PlatformCode.Should().Be("WooCommerce");
    }

    // ─── 2. TestConnectionAsync — valid credentials ────────────

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange: system_status returns OK, products count header present
        var systemStatusResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"environment":{"site_url":"https://mystore.example.com"}}""",
                Encoding.UTF8, "application/json")
        };

        var productsCountResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        productsCountResponse.Headers.Add("X-WP-Total", "42");

        var callCount = 0;
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? systemStatusResponse : productsCountResponse;
            });

        // Act
        var result = await _sut.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("WooCommerce");
        result.StoreName.Should().Be("https://mystore.example.com");
        result.ProductCount.Should().Be(42);
    }

    // ─── 3. TestConnectionAsync — invalid / missing credentials ─

    [Fact]
    public async Task TestConnectionAsync_InvalidCredentials_ShouldReturnFailure()
    {
        // Act — empty credentials → _isConfigured stays false
        var result = await _sut.TestConnectionAsync(InvalidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("eksik");
    }

    // ─── 4. PushProductAsync — unconfigured ────────────────────

    [Fact]
    public async Task PushProductAsync_Unconfigured_ShouldReturnFalse()
    {
        // PushProductAsync does not call EnsureConfigured — it returns false (not yet implemented)
        var product = new MesTech.Domain.Entities.Product { Name = "Test", SKU = "WC-001" };

        var result = await _sut.PushProductAsync(product);

        result.Should().BeFalse();
    }

    // ─── 5. PushStockUpdateAsync — unconfigured ────────────────

    [Fact]
    public async Task PushStockUpdateAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        // Adapter not configured → EnsureConfigured throws
        Func<Task> act = () => _sut.PushStockUpdateAsync(Guid.NewGuid(), 10);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    // ─── 6. PullProductsAsync — unconfigured ──────────────────

    [Fact]
    public async Task PullProductsAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        Func<Task> act = () => _sut.PullProductsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    // ─── 7. BatchUpdateProductsAsync — unconfigured ────────────

    [Fact]
    public async Task BatchUpdateProductsAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var updates = new List<MesTech.Application.DTOs.Platform.BatchProductUpdateDto>
        {
            new() { ProductId = 1, Stock = 5 }
        };

        Func<Task> act = () => _sut.BatchUpdateProductsAsync(updates);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    // ─── 8. TestConnectionAsync — HTTP 401 from WooCommerce ───

    [Fact]
    public async Task TestConnectionAsync_Unauthorized_ShouldReturnFailureWithHttpStatus()
    {
        SetupResponse(HttpStatusCode.Unauthorized,
            """{"code":"woocommerce_rest_cannot_view","message":"Sorry, you cannot list resources."}""");

        var result = await _sut.TestConnectionAsync(ValidCredentials);

        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("Unauthorized");
    }

    // ─── 9. SupportsStockUpdate / SupportsPriceUpdate flags ───

    [Fact]
    public void CapabilityFlags_ShouldBeTrue()
    {
        _sut.SupportsStockUpdate.Should().BeTrue();
        _sut.SupportsPriceUpdate.Should().BeTrue();
        _sut.SupportsShipment.Should().BeTrue();
    }
}
