using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// ZalandoAdapter unit testleri — HTTP çağrısı yapmadan temel davranış kontrolü.
/// G015: Zalando adapter 0 test → temel TestConnection/Ping/Properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Platform", "Zalando")]
public class ZalandoAdapterUnitTests
{
    private readonly Mock<ILogger<ZalandoAdapter>> _logger = new();

    private ZalandoAdapter CreateAdapter(ZalandoOptions? options = null)
    {
        var httpClient = new HttpClient(new FakeHandler());
        if (options != null)
            return new ZalandoAdapter(httpClient, _logger.Object, Options.Create(options));
        return new ZalandoAdapter(httpClient, _logger.Object);
    }

    private static ZalandoOptions ValidOptions() => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        Enabled = true
    };

    [Fact]
    public void PlatformCode_IsZalando()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("Zalando");
    }

    [Fact]
    public void SupportsStockUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsPriceUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_EmptyCredentials_ReturnsFailure()
    {
        var adapter = CreateAdapter();
        var emptyCredentials = new Dictionary<string, string>();

        var result = await adapter.TestConnectionAsync(emptyCredentials);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PullProducts_NotConfigured_ThrowsInvalidOperation()
    {
        var adapter = CreateAdapter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.PullProductsAsync());
    }

    [Fact]
    public async Task PushStockUpdate_NotConfigured_ThrowsInvalidOperation()
    {
        var adapter = CreateAdapter();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.PushStockUpdateAsync(Guid.NewGuid(), 10));
    }

    [Fact]
    public async Task Ping_NetworkError_ReturnsFalse()
    {
        var adapter = CreateAdapter(ValidOptions());
        var result = await adapter.PingAsync();
        result.Should().BeFalse();
    }

    /// <summary>Her zaman exception fırlatan fake HTTP handler.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network failure");
        }
    }
}
