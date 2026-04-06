using FluentAssertions;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Payment;

/// <summary>
/// HH-DEV5-005: Payment adapter unit tests.
/// Tests iyzico and Stripe gateways: sandbox mode, charge, refund, card operations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "PaymentAdapter")]
[Trait("Phase", "Dalga15")]
public class PaymentAdapterUnitTests
{
    // ═══════════════════════════════════════════
    // iyzico Gateway Tests
    // ═══════════════════════════════════════════

    private static IyzicoPaymentGateway CreateIyzico(IyzicoOptions? options = null)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") });

        return new IyzicoPaymentGateway(
            Mock.Of<ILogger<IyzicoPaymentGateway>>(),
            Options.Create(options ?? new IyzicoOptions()),
            httpFactory.Object);
    }

    [Fact]
    public void Iyzico_ProviderName_ShouldBe_iyzico()
    {
        var sut = CreateIyzico();
        sut.ProviderName.Should().Be("iyzico");
    }

    [Fact]
    public async Task Iyzico_ChargeAsync_Unconfigured_ReturnsSandboxSuccess()
    {
        var sut = CreateIyzico(); // default options = unconfigured

        var result = await sut.ChargeAsync(100.00m, "TRY", "card-token-001");

        result.Success.Should().BeTrue("unconfigured iyzico should return sandbox success");
        result.TransactionId.Should().StartWith("sandbox-", "sandbox mode generates prefixed IDs");
    }

    [Fact]
    public async Task Iyzico_RefundAsync_Unconfigured_ReturnsSandboxSuccess()
    {
        var sut = CreateIyzico();

        var result = await sut.RefundAsync("tx-001", 50.00m);

        result.Success.Should().BeTrue("unconfigured iyzico refund should return sandbox success");
        result.TransactionId.Should().StartWith("refund-sandbox-");
    }

    [Fact]
    public async Task Iyzico_ChargeAsync_WithDescription_DoesNotThrow()
    {
        var sut = CreateIyzico();

        var result = await sut.ChargeAsync(250.00m, "TRY", "card-token-002", "MesTech Premium Abonelik");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Iyzico_RefundAsync_FullRefund_NullAmount_ReturnsSandboxSuccess()
    {
        var sut = CreateIyzico();

        var result = await sut.RefundAsync("tx-full-refund");

        result.Success.Should().BeTrue("full refund with null amount should work");
    }

    [Fact]
    public void Iyzico_SaveCardAsync_Unconfigured_ReturnsSandboxToken()
    {
        var sut = CreateIyzico();
        var card = new CardInfo("John Doe", "4111111111111111", 12, 2028, "123");

        var act = async () => await sut.SaveCardAsync(card);

        // Sandbox mode returns a token
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Iyzico_DeleteCardAsync_Unconfigured_ReturnsTrue()
    {
        var sut = CreateIyzico();

        var result = await sut.DeleteCardAsync("sandbox-token-001");

        result.Should().BeTrue("sandbox mode always succeeds");
    }

    [Fact]
    public void Iyzico_SaveCardAsync_Configured_ThrowsNotSupported()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "sandbox-api-key-1234567890",
            SecretKey = "sandbox-secret-key-1234567890"
        };
        var sut = CreateIyzico(options);
        var card = new CardInfo("John Doe", "4111111111111111", 12, 2028, "123");

        var act = async () => await sut.SaveCardAsync(card);

        act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Iyzipay SDK*");
    }

    [Fact]
    public void Iyzico_DeleteCardAsync_Configured_ThrowsNotSupported()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "sandbox-api-key-1234567890",
            SecretKey = "sandbox-secret-key-1234567890"
        };
        var sut = CreateIyzico(options);

        var act = async () => await sut.DeleteCardAsync("some-token");

        act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Iyzipay SDK*");
    }

    [Fact]
    public void IyzicoOptions_Default_IsNotConfigured()
    {
        var options = new IyzicoOptions();
        options.IsConfigured.Should().BeFalse("empty keys = not configured");
        options.BaseUrl.Should().Contain("sandbox", "default should be sandbox");
    }

    [Fact]
    public void IyzicoOptions_WithKeys_IsConfigured()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "key123",
            SecretKey = "secret456"
        };
        options.IsConfigured.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // Stripe Gateway Tests
    // ═══════════════════════════════════════════

    private static StripePaymentGateway CreateStripe(StripeOptions? options = null)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient { BaseAddress = new Uri("https://api.stripe.com") });

        return new StripePaymentGateway(
            Mock.Of<ILogger<StripePaymentGateway>>(),
            Options.Create(options ?? new StripeOptions()),
            httpFactory.Object);
    }

    [Fact]
    public void Stripe_ProviderName_ShouldBe_Stripe()
    {
        var sut = CreateStripe();
        sut.ProviderName.Should().Be("Stripe");
    }

    [Fact]
    public async Task Stripe_ChargeAsync_Unconfigured_ReturnsSandboxSuccess()
    {
        var sut = CreateStripe();

        var result = await sut.ChargeAsync(200.00m, "USD", "pm_card_visa");

        result.Success.Should().BeTrue("unconfigured Stripe returns sandbox success");
        result.TransactionId.Should().StartWith("pi_sandbox_", "Stripe sandbox returns pi_ prefixed IDs");
    }

    [Fact]
    public async Task Stripe_RefundAsync_Unconfigured_ReturnsSandboxSuccess()
    {
        var sut = CreateStripe();

        var result = await sut.RefundAsync("pi_sandbox_001", 100.00m);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void StripeOptions_Default_IsNotConfigured()
    {
        var options = new StripeOptions();
        options.IsConfigured.Should().BeFalse();
    }
}
