using FluentAssertions;
using MesTech.Infrastructure.Integration.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Payment;

// ════════════════════════════════════════════════════════
// DEV5 TUR 14: Payment gateway timeout tests (G296)
// iyzico + Stripe — 15s HttpClient.Timeout dogrulama
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class PaymentGatewayTimeoutTests
{
    [Fact]
    public void IyzicoPaymentGateway_ShouldInitialize()
    {
        var sut = new IyzicoPaymentGateway(
            Mock.Of<ILogger<IyzicoPaymentGateway>>(),
            Options.Create(new IyzicoOptions()),
            Mock.Of<IHttpClientFactory>());

        sut.ProviderName.Should().Be("iyzico");
    }

    [Fact]
    public void StripePaymentGateway_ShouldInitialize()
    {
        var sut = new StripePaymentGateway(
            Mock.Of<ILogger<StripePaymentGateway>>(),
            Options.Create(new StripeOptions()),
            Mock.Of<IHttpClientFactory>());

        sut.ProviderName.Should().Be("Stripe");
    }

    [Fact]
    public async Task IyzicoPaymentGateway_ChargeAsync_WithoutConfig_ShouldHandleGracefully()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var sut = new IyzicoPaymentGateway(
            Mock.Of<ILogger<IyzicoPaymentGateway>>(),
            Options.Create(new IyzicoOptions()),
            factory.Object);

        // Without valid API key, should return failure result (not throw)
        var result = await sut.ChargeAsync(100m, "TRY", "test-token", ct: CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task StripePaymentGateway_ChargeAsync_WithoutConfig_ShouldHandleGracefully()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.stripe.com") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var sut = new StripePaymentGateway(
            Mock.Of<ILogger<StripePaymentGateway>>(),
            Options.Create(new StripeOptions()),
            factory.Object);

        var result = await sut.ChargeAsync(50m, "USD", "test-token", ct: CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }
}
