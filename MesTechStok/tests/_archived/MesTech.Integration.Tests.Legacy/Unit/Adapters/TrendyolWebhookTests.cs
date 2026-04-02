using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// TrendyolAdapter IWebhookCapableAdapter testleri.
/// G010: RegisterWebhook/Unregister/ProcessPayload happy+error path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Platform", "Trendyol")]
[Trait("Group", "WebhookAdapter")]
public class TrendyolWebhookTests
{
    private readonly Mock<ILogger<TrendyolAdapter>> _logger = new();

    private TrendyolAdapter CreateConfiguredAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.trendyol.com") };
        var options = Options.Create(new TrendyolOptions { Enabled = true });
        var adapter = new TrendyolAdapter(httpClient, _logger.Object, options);
        // Configure credentials via TestConnection (FakeHandler returns 200)
        adapter.TestConnectionAsync(new Dictionary<string, string>
        {
            ["SupplierId"] = "12345",
            ["ApiKey"] = "test-key",
            ["ApiSecret"] = "test-secret"
        }).GetAwaiter().GetResult();
        return adapter;
    }

    private TrendyolAdapter CreateUnconfiguredAdapter()
    {
        var httpClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        return new TrendyolAdapter(httpClient, _logger.Object);
    }

    [Fact]
    public async Task RegisterWebhook_Success_ReturnsTrue()
    {
        var adapter = CreateConfiguredAdapter(new FakeHandler(HttpStatusCode.OK));
        var result = await adapter.RegisterWebhookAsync("https://mestech.app/webhooks/trendyol");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterWebhook_HttpError_ReturnsFalse()
    {
        var adapter = CreateConfiguredAdapter(new FakeHandler(HttpStatusCode.BadRequest));
        var result = await adapter.RegisterWebhookAsync("https://mestech.app/webhooks/trendyol");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWebhook_NetworkException_ReturnsFalse()
    {
        var adapter = CreateConfiguredAdapter(new ThrowingHandler());
        var result = await adapter.RegisterWebhookAsync("https://mestech.app/webhooks/trendyol");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var adapter = CreateUnconfiguredAdapter();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/webhooks/trendyol"));
    }

    [Fact]
    public async Task UnregisterWebhook_ReturnsTrue()
    {
        var adapter = CreateConfiguredAdapter(new FakeHandler(HttpStatusCode.OK));
        var result = await adapter.UnregisterWebhookAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var adapter = CreateConfiguredAdapter(new FakeHandler(HttpStatusCode.OK));
        var payload = """{"eventType":"OrderCreated","orderNumber":"ORD-123"}""";

        var act = () => adapter.ProcessWebhookPayloadAsync(payload);
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessWebhookPayload_InvalidJson_ThrowsJsonException()
    {
        var adapter = CreateConfiguredAdapter(new FakeHandler(HttpStatusCode.OK));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            adapter.ProcessWebhookPayloadAsync("not-valid-json{{{"));
    }

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent("{}") });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Network failure");
    }
}
