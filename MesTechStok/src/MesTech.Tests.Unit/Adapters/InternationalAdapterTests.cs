using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// Dalga 14 Sprint 2 — International adapter unit tests.
/// Tests constructor guards, PlatformCode, capability flags, and unconfigured guards.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "InternationalAdapters")]
[Trait("Phase", "Dalga14")]
public class InternationalAdapterTests
{
    // ═══════════════════════════════════════════
    // EbayAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void EbayAdapter_NullHttpClient_Throws()
    {
        var logger = NullLogger<EbayAdapter>.Instance;
        var act = () => new EbayAdapter(null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void EbayAdapter_NullLogger_Throws()
    {
        var client = new HttpClient();
        var act = () => new EbayAdapter(client, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void EbayAdapter_PlatformCode_IsEBay()
    {
        var sut = CreateEbayAdapter();
        sut.PlatformCode.Should().Be("eBay");
    }

    [Fact]
    public void EbayAdapter_SupportsStockUpdate_True()
    {
        var sut = CreateEbayAdapter();
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void EbayAdapter_SupportsPriceUpdate_True()
    {
        var sut = CreateEbayAdapter();
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public void EbayAdapter_SupportsShipment_True()
    {
        var sut = CreateEbayAdapter();
        sut.SupportsShipment.Should().BeTrue();
    }

    [Fact]
    public async Task EbayAdapter_TestConnectionAsync_NullCredentials_Throws()
    {
        var sut = CreateEbayAdapter();
        var act = () => sut.TestConnectionAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EbayAdapter_TestConnectionAsync_MissingCredentials_ReturnsFailure()
    {
        var sut = CreateEbayAdapter();
        var creds = new Dictionary<string, string>
        {
            ["ClientId"] = "",
            ["ClientSecret"] = ""
        };

        var result = await sut.TestConnectionAsync(creds);

        result.IsSuccess.Should().BeFalse();
        result.PlatformCode.Should().Be("eBay");
    }

    [Fact]
    public async Task EbayAdapter_PullProductsAsync_Unconfigured_ThrowsInvalidOperation()
    {
        var sut = CreateEbayAdapter();
        var act = () => sut.PullProductsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EbayAdapter_PushStockUpdateAsync_Unconfigured_ThrowsInvalidOperation()
    {
        var sut = CreateEbayAdapter();
        var act = () => sut.PushStockUpdateAsync(Guid.NewGuid(), 10);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EbayAdapter_PushPriceUpdateAsync_Unconfigured_ThrowsInvalidOperation()
    {
        var sut = CreateEbayAdapter();
        var act = () => sut.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EbayAdapter_PushProductAsync_ReturnsFalse()
    {
        // PushProductAsync is a stub that returns false (3-step flow not implemented)
        var sut = CreateEbayAdapter();
        var product = new MesTech.Domain.Entities.Product { Name = "Test", SalePrice = 100m };
        var result = await sut.PushProductAsync(product);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EbayAdapter_UpdateOrderStatusAsync_AlwaysFalse()
    {
        // eBay doesn't support direct order status updates
        var sut = CreateEbayAdapter();
        var result = await sut.UpdateOrderStatusAsync("PKG-123", "Shipped");
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // EtsyAdapter Tests (Stub)
    // ═══════════════════════════════════════════

    [Fact]
    public void EtsyAdapter_NullLogger_Throws()
    {
        var act = () => new EtsyAdapter(new HttpClient(), null!, null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void EtsyAdapter_PlatformCode_IsEtsy()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        sut.PlatformCode.Should().Be("Etsy");
    }

    [Fact]
    public void EtsyAdapter_SupportsStockUpdate_True()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void EtsyAdapter_SupportsPriceUpdate_True()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public void EtsyAdapter_SupportsShipment_False()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        sut.SupportsShipment.Should().BeFalse();
    }

    [Fact]
    public async Task EtsyAdapter_TestConnectionAsync_ReturnsStubFailure()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        var result = await sut.TestConnectionAsync(new Dictionary<string, string>());
        result.IsSuccess.Should().BeFalse();
        result.PlatformCode.Should().Be("Etsy");
    }

    [Fact]
    public async Task EtsyAdapter_PullProductsAsync_ThrowsWhenNotConfigured()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        var act = () => sut.PullProductsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EtsyAdapter_PushProductAsync_ThrowsWhenNotConfigured()
    {
        var sut = new EtsyAdapter(new HttpClient(), NullLogger<EtsyAdapter>.Instance, null);
        var act = () => sut.PushProductAsync(new MesTech.Domain.Entities.Product());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ═══════════════════════════════════════════
    // ZalandoAdapter Tests (Stub)
    // ═══════════════════════════════════════════

    [Fact]
    public void ZalandoAdapter_NullLogger_Throws()
    {
        var act = () => new ZalandoAdapter(new HttpClient(), null!, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ZalandoAdapter_PlatformCode_IsZalando()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        sut.PlatformCode.Should().Be("Zalando");
    }

    [Fact]
    public void ZalandoAdapter_SupportsStockUpdate_True()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void ZalandoAdapter_SupportsPriceUpdate_True()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public void ZalandoAdapter_SupportsShipment_False()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        sut.SupportsShipment.Should().BeFalse();
    }

    [Fact]
    public async Task ZalandoAdapter_TestConnectionAsync_ReturnsStubFailure()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        var result = await sut.TestConnectionAsync(new Dictionary<string, string>());
        result.IsSuccess.Should().BeFalse();
        result.PlatformCode.Should().Be("Zalando");
    }

    [Fact]
    public async Task ZalandoAdapter_PullProductsAsync_Unconfigured_ThrowsInvalidOperation()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        var act = () => sut.PullProductsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ZalandoAdapter_PushProductAsync_ReturnsFalse()
    {
        var sut = new ZalandoAdapter(new HttpClient(), NullLogger<ZalandoAdapter>.Instance, null);
        var result = await sut.PushProductAsync(new MesTech.Domain.Entities.Product());
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // OzonAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void OzonAdapter_NullHttpClient_Throws()
    {
        var logger = NullLogger<OzonAdapter>.Instance;
        var act = () => new OzonAdapter(null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void OzonAdapter_NullLogger_Throws()
    {
        var client = new HttpClient();
        var act = () => new OzonAdapter(client, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void OzonAdapter_PlatformCode_IsOzon()
    {
        var sut = new OzonAdapter(new HttpClient(), NullLogger<OzonAdapter>.Instance);
        sut.PlatformCode.Should().Be("Ozon");
    }

    [Fact]
    public void OzonAdapter_SupportsStockUpdate_True()
    {
        var sut = new OzonAdapter(new HttpClient(), NullLogger<OzonAdapter>.Instance);
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void OzonAdapter_SupportsPriceUpdate_True()
    {
        var sut = new OzonAdapter(new HttpClient(), NullLogger<OzonAdapter>.Instance);
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // PttAvmAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void PttAvmAdapter_NullHttpClient_Throws()
    {
        var act = () => new PttAvmAdapter(null!, NullLogger<PttAvmAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void PttAvmAdapter_NullLogger_Throws()
    {
        var act = () => new PttAvmAdapter(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void PttAvmAdapter_PlatformCode_IsPttAvm()
    {
        var sut = new PttAvmAdapter(new HttpClient(), NullLogger<PttAvmAdapter>.Instance);
        sut.PlatformCode.Should().NotBeNullOrWhiteSpace();
    }

    // ═══════════════════════════════════════════
    // MngKargoAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void MngKargoAdapter_NullHttpClient_Throws()
    {
        var act = () => new MngKargoAdapter(null!, NullLogger<MngKargoAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void MngKargoAdapter_NullLogger_Throws()
    {
        var act = () => new MngKargoAdapter(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ═══════════════════════════════════════════
    // WooCommerceAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void WooCommerceAdapter_NullHttpClient_Throws()
    {
        var act = () => new WooCommerceAdapter(null!, NullLogger<WooCommerceAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void WooCommerceAdapter_NullLogger_Throws()
    {
        var act = () => new WooCommerceAdapter(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void WooCommerceAdapter_PlatformCode_IsWooCommerce()
    {
        var sut = new WooCommerceAdapter(new HttpClient(), NullLogger<WooCommerceAdapter>.Instance);
        sut.PlatformCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WooCommerceAdapter_SupportsStockUpdate_True()
    {
        var sut = new WooCommerceAdapter(new HttpClient(), NullLogger<WooCommerceAdapter>.Instance);
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void WooCommerceAdapter_SupportsPriceUpdate_True()
    {
        var sut = new WooCommerceAdapter(new HttpClient(), NullLogger<WooCommerceAdapter>.Instance);
        sut.SupportsPriceUpdate.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // ShopifyAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void ShopifyAdapter_NullHttpClient_Throws()
    {
        var act = () => new ShopifyAdapter(null!, NullLogger<ShopifyAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void ShopifyAdapter_NullLogger_Throws()
    {
        var act = () => new ShopifyAdapter(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void ShopifyAdapter_PlatformCode_IsShopify()
    {
        var sut = new ShopifyAdapter(new HttpClient(), NullLogger<ShopifyAdapter>.Instance);
        sut.PlatformCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShopifyAdapter_SupportsStockUpdate_True()
    {
        var sut = new ShopifyAdapter(new HttpClient(), NullLogger<ShopifyAdapter>.Instance);
        sut.SupportsStockUpdate.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // AmazonEuAdapter Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void AmazonEuAdapter_NullHttpClient_Throws()
    {
        var act = () => new AmazonEuAdapter(null!, NullLogger<AmazonEuAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void AmazonEuAdapter_NullLogger_Throws()
    {
        var act = () => new AmazonEuAdapter(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void AmazonEuAdapter_PlatformCode_IsAmazon()
    {
        var sut = new AmazonEuAdapter(new HttpClient(), NullLogger<AmazonEuAdapter>.Instance);
        sut.PlatformCode.Should().NotBeNullOrWhiteSpace();
    }

    // ═══════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════

    private static EbayAdapter CreateEbayAdapter()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        var httpClient = new HttpClient(mockHandler.Object);
        return new EbayAdapter(httpClient, NullLogger<EbayAdapter>.Instance);
    }
}
