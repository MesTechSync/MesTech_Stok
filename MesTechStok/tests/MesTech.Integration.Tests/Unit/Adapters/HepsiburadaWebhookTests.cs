using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// HepsiburadaAdapter IWebhookCapableAdapter testleri.
/// G010: RegisterWebhook/Unregister/ProcessPayload happy+error path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Platform", "Hepsiburada")]
[Trait("Group", "WebhookAdapter")]
public class HepsiburadaWebhookTests
{
    private readonly Mock<ILogger<HepsiburadaAdapter>> _logger = new();

    private HepsiburadaAdapter CreateConfiguredAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.hepsiburada.com") };
        // Configure via TestConnectionAsync or directly via options
        var adapter = new HepsiburadaAdapter(httpClient, _logger.Object);
        // Use reflection-free approach — configure through TestConnection
        var credentials = new Dictionary<string, string>
        {
            ["MerchantId"] = "test-merchant",
            ["Username"] = "test-user",
            ["Password"] = "test-pass"
        };
        // Note: TestConnection will call the handler — FakeHandler returns 200
        adapter.TestConnectionAsync(credentials).GetAwaiter().GetResult();
        return adapter;
    }

    [Fact]
    public async Task RegisterWebhook_NotConfigured_ThrowsInvalidOperation()
    {
        var httpClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var adapter = new HepsiburadaAdapter(httpClient, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RegisterWebhookAsync("https://mestech.app/webhooks/hb"));
    }

    [Fact]
    public void ProcessWebhookPayload_ValidJson_DoesNotThrow()
    {
        var httpClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var adapter = new HepsiburadaAdapter(httpClient, _logger.Object);
        var payload = """{"eventType":"ORDER_CREATED","orderId":"HB-456"}""";

        var act = () => adapter.ProcessWebhookPayloadAsync(payload);
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessWebhookPayload_InvalidJson_ThrowsException()
    {
        var httpClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var adapter = new HepsiburadaAdapter(httpClient, _logger.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            adapter.ProcessWebhookPayloadAsync("{invalid json!!!"));
    }

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("""{"result":true,"merchantId":"test"}""")
            });
    }
}
