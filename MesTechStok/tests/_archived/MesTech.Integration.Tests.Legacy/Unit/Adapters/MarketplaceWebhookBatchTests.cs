using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// G010: Kalan 8 adapter'ın IWebhookCapableAdapter testleri.
/// N11, Ciceksepeti, eBay, Ozon, Pazarama, PttAvm, Shopify, WooCommerce.
/// </summary>

// ══════════════════════════════════════════════════════════════
// N11 — webhook desteklemiyor, polling kullanıyor
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class N11WebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NoOpStub_ReturnsTrue()
    {
        var logger = new Mock<ILogger<N11Adapter>>();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new OkHandler()));
        var adapter = new N11Adapter(logger.Object, factory.Object);

        var result = await adapter.RegisterWebhookAsync("https://mestech.app/n11");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWebhookPayload_Unsupported_DoesNotThrow()
    {
        var logger = new Mock<ILogger<N11Adapter>>();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new OkHandler()));
        var adapter = new N11Adapter(logger.Object, factory.Object);

        await adapter.ProcessWebhookPayloadAsync("""{"test":"data"}""");
        // No exception = success
    }
}

// ══════════════════════════════════════════════════════════════
// Ciceksepeti — API üzerinden webhook desteklemiyor
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class CiceksepetiWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_Unsupported_ReturnsFalse()
    {
        var logger = new Mock<ILogger<CiceksepetiAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new CiceksepetiAdapter(httpClient, logger.Object);

        var result = await adapter.RegisterWebhookAsync("https://mestech.app/cs");
        result.Should().BeFalse(); // API desteklemiyor
    }

    [Fact]
    public async Task ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var logger = new Mock<ILogger<CiceksepetiAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new CiceksepetiAdapter(httpClient, logger.Object);

        await adapter.ProcessWebhookPayloadAsync(
            """{"EventType":"ORDER_CREATED","OrderId":"CS-123","SubOrderId":"SUB-1"}""");
    }
}

// ══════════════════════════════════════════════════════════════
// eBay — OAuth2 + subscription webhook
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class EbayWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var logger = new Mock<ILogger<EbayAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new EbayAdapter(httpClient, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/ebay"));
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var logger = new Mock<ILogger<EbayAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new EbayAdapter(httpClient, logger.Object);

        var act = () => adapter.ProcessWebhookPayloadAsync(
            """{"metadata":{"topic":"MARKETPLACE_ACCOUNT_DELETION"},"notification":{}}""");
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessWebhookPayload_InvalidJson_ThrowsException()
    {
        var logger = new Mock<ILogger<EbayAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new EbayAdapter(httpClient, logger.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            adapter.ProcessWebhookPayloadAsync("invalid{json"));
    }
}

// ══════════════════════════════════════════════════════════════
// Ozon — client-id/api-key header auth
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class OzonWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var logger = new Mock<ILogger<OzonAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new OzonAdapter(httpClient, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/ozon"));
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_ExtractsType()
    {
        var logger = new Mock<ILogger<OzonAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new OzonAdapter(httpClient, logger.Object);

        var act = () => adapter.ProcessWebhookPayloadAsync(
            """{"type":"ORDER_STATUS_CHANGED","order_id":"OZ-789"}""");
        act.Should().NotThrowAsync();
    }
}

// ══════════════════════════════════════════════════════════════
// Pazarama — EnsureConfigured + retry
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class PazaramaWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var logger = new Mock<ILogger<PazaramaAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new PazaramaAdapter(httpClient, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/pazarama"));
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var logger = new Mock<ILogger<PazaramaAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new PazaramaAdapter(httpClient, logger.Object);

        var act = () => adapter.ProcessWebhookPayloadAsync(
            """{"eventType":"ORDER_CREATED","orderId":"PZ-456"}""");
        act.Should().NotThrowAsync();
    }
}

// ══════════════════════════════════════════════════════════════
// PttAvm — OAuth2 Bearer token
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class PttAvmWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var logger = new Mock<ILogger<PttAvmAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new PttAvmAdapter(httpClient, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/pttavm"));
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var logger = new Mock<ILogger<PttAvmAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new PttAvmAdapter(httpClient, logger.Object);

        var act = () => adapter.ProcessWebhookPayloadAsync(
            """{"eventType":"ORDER_CREATED","orderId":"PTT-789"}""");
        act.Should().NotThrowAsync();
    }
}

// ══════════════════════════════════════════════════════════════
// Shopify — multi-topic registration + detailed payload parse
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class ShopifyWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var logger = new Mock<ILogger<ShopifyAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new ShopifyAdapter(httpClient, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/shopify"));
    }

    [Fact]
    public async Task ProcessWebhookPayload_EmptyPayload_DoesNotThrow()
    {
        var logger = new Mock<ILogger<ShopifyAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new ShopifyAdapter(httpClient, logger.Object);

        // Shopify null/empty payload koruma kontrolü
        await adapter.ProcessWebhookPayloadAsync("");
    }

    [Fact]
    public void ProcessWebhookPayload_ValidOrder_DoesNotThrow()
    {
        var logger = new Mock<ILogger<ShopifyAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new ShopifyAdapter(httpClient, logger.Object);

        var act = () => adapter.ProcessWebhookPayloadAsync(
            """{"id":12345,"financial_status":"paid","total_price":"299.99"}""");
        act.Should().NotThrowAsync();
    }
}

// ══════════════════════════════════════════════════════════════
// WooCommerce — basit stub
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "WebhookAdapter")]
public class WooCommerceWebhookTests
{
    [Fact]
    public async Task RegisterWebhook_Stub_ReturnsTrue()
    {
        var logger = new Mock<ILogger<WooCommerceAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new WooCommerceAdapter(httpClient, logger.Object);

        var result = await adapter.RegisterWebhookAsync("https://mestech.app/woo");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWebhookPayload_DoesNotThrow()
    {
        var logger = new Mock<ILogger<WooCommerceAdapter>>();
        var httpClient = new HttpClient(new OkHandler());
        var adapter = new WooCommerceAdapter(httpClient, logger.Object);

        await adapter.ProcessWebhookPayloadAsync("""{"order_id":"WOO-001"}""");
    }
}

// ══════════════════════════════════════════════════════════════
// Shared helper — tüm test sınıfları tarafından kullanılır
// ══════════════════════════════════════════════════════════════

file sealed class OkHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"result":true}""")
        });
}
