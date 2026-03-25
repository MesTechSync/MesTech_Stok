using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// AmazonEuAdapter IWebhookCapableAdapter testleri.
/// G010: SNS stub — RegisterWebhook her zaman true döner.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Platform", "AmazonEu")]
[Trait("Group", "WebhookAdapter")]
public class AmazonEuWebhookTests
{
    private readonly Mock<ILogger<AmazonEuAdapter>> _logger = new();

    private AmazonEuAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(new FakeHandler()) { BaseAddress = new Uri("https://sellingpartnerapi-eu.amazon.com") };
        return new AmazonEuAdapter(httpClient, _logger.Object);
    }

    [Fact]
    public async Task RegisterWebhook_SNSStub_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        var result = await adapter.RegisterWebhookAsync("https://mestech.app/webhooks/amazon");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisterWebhook_SNSStub_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        var result = await adapter.UnregisterWebhookAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWebhookPayload_SNSStub_DoesNotThrow()
    {
        var adapter = CreateAdapter();
        var payload = """{"Type":"Notification","Message":"order-update"}""";

        await adapter.ProcessWebhookPayloadAsync(payload);
        // No exception = success (SNS stub)
    }

    [Fact]
    public void PlatformCode_IsAmazonEU()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("AmazonEU");
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
    }
}
