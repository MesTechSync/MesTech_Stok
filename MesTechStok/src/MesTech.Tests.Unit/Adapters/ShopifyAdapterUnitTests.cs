using System.Net;
using System.Text;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

[Trait("Category", "Unit")]
[Trait("Platform", "Shopify")]
public class ShopifyAdapterUnitTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handler;
    private readonly HttpClient _httpClient;

    public ShopifyAdapterUnitTests()
    {
        _handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_handler.Object)
        {
            BaseAddress = new Uri("https://test-store.myshopify.com")
        };
    }

    public void Dispose() => _httpClient.Dispose();

    // ── Helpers ──────────────────────────────────────────────────

    private ShopifyAdapter CreateAdapter(ShopifyOptions? opts = null)
    {
        var options = Options.Create(opts ?? new ShopifyOptions());
        return new ShopifyAdapter(_httpClient, NullLogger<ShopifyAdapter>.Instance, options);
    }

    private ShopifyAdapter CreateConfiguredAdapter()
    {
        var opts = new ShopifyOptions
        {
            ShopDomain = "test-store.myshopify.com",
            AccessToken = "shpat_test_token_123",
            LocationId = "12345678",
            WebhookSecret = "whsec_test_secret"
        };
        return CreateAdapter(opts);
    }

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

    private void SetupSequentialResponses(params (HttpStatusCode status, string body)[] responses)
    {
        var setup = _handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

        foreach (var (status, body) in responses)
        {
            setup.ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    // ── 1. PlatformCode ─────────────────────────────────────────

    [Fact]
    public void PlatformCode_ShouldBeShopify()
    {
        var sut = CreateAdapter();
        sut.PlatformCode.Should().Be("Shopify");
    }

    // ── 2. TestConnectionAsync — valid credentials ──────────────

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange: first call = shop.json, second call = products/count.json
        SetupSequentialResponses(
            (HttpStatusCode.OK, """{"shop":{"name":"My Test Store","id":123456}}"""),
            (HttpStatusCode.OK, """{"count":42}""")
        );

        var sut = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["ShopDomain"] = "test-store.myshopify.com",
            ["AccessToken"] = "shpat_valid_token"
        };

        // Act
        var result = await sut.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Shopify");
        result.StoreName.Should().Be("My Test Store");
        result.ProductCount.Should().Be(42);
    }

    // ── 3. TestConnectionAsync — invalid credentials ────────────

    [Fact]
    public async Task TestConnectionAsync_InvalidCredentials_ShouldReturnFailure()
    {
        // Arrange: Shopify returns 401 for bad token
        SetupResponse(HttpStatusCode.Unauthorized, """{"errors":"[API] Invalid API key or access token"}""");

        var sut = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["ShopDomain"] = "test-store.myshopify.com",
            ["AccessToken"] = "shpat_bad_token"
        };

        // Act
        var result = await sut.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.HttpStatusCode.Should().Be(401);
    }

    // ── 4. PushProductAsync — unconfigured returns false ────────

    [Fact]
    public async Task PushProductAsync_Unconfigured_ShouldReturnFalse()
    {
        // ShopifyAdapter.PushProductAsync does NOT call EnsureConfigured;
        // it always returns false (full product creation not supported).
        var sut = CreateAdapter();
        var product = new Product { Name = "Test", SKU = "TST-001" };

        var result = await sut.PushProductAsync(product);

        result.Should().BeFalse();
    }

    // ── 5. PushStockUpdateAsync — unconfigured throws ───────────

    [Fact]
    public async Task PushStockUpdateAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.PushStockUpdateAsync(Guid.NewGuid(), 10);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }

    // ── 6. PullProductsAsync — unconfigured throws ──────────────

    [Fact]
    public async Task PullProductsAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.PullProductsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }

    // ── 7. RegisterWebhookAsync — unconfigured throws ───────────

    [Fact]
    public async Task RegisterWebhookAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.RegisterWebhookAsync("https://example.com/webhook");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }

    // ── 8. TestConnectionAsync — missing credentials ────────────

    [Fact]
    public async Task TestConnectionAsync_MissingShopDomain_ShouldReturnFailure()
    {
        var sut = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["AccessToken"] = "shpat_valid_token"
            // ShopDomain missing
        };

        var result = await sut.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("eksik");
    }

    // ── 9. PullOrdersAsync — unconfigured throws ────────────────

    [Fact]
    public async Task PullOrdersAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.PullOrdersAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }

    // ── 10. SupportsStockUpdate — should be true ────────────────

    [Fact]
    public void SupportsStockUpdate_ShouldBeTrue()
    {
        var sut = CreateAdapter();
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    // ── 11. SupportsPriceUpdate — should be true ────────────────

    [Fact]
    public void SupportsPriceUpdate_ShouldBeTrue()
    {
        var sut = CreateAdapter();
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    // ── 12. PushPriceUpdateAsync — unconfigured throws ──────────

    [Fact]
    public async Task PushPriceUpdateAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.PushPriceUpdateAsync(Guid.NewGuid(), 29.99m);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }

    // ── 13. GetCategoriesAsync — unconfigured throws ────────────

    [Fact]
    public async Task GetCategoriesAsync_Unconfigured_ShouldThrowInvalidOperation()
    {
        var sut = CreateAdapter();

        var act = () => sut.GetCategoriesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*henuz yapilandirilmadi*");
    }
}
