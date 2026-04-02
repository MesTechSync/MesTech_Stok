using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Webhooks;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Webhooks;

public class WebhookReceiverTests
{
    private readonly Mock<ILogger<WebhookReceiverService>> _loggerMock = new();

    [Fact]
    public async Task ProcessOrderWebhook_RoutesToAdapter()
    {
        // Arrange — mock that implements BOTH IIntegratorAdapter AND IWebhookCapableAdapter
        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.PlatformCode).Returns("Trendyol");

        var webhookCapable = mockAdapter.As<IWebhookCapableAdapter>();
        webhookCapable
            .Setup(w => w.ProcessWebhookPayloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var adapters = new List<IIntegratorAdapter> { mockAdapter.Object };
        var service = new WebhookReceiverService(adapters, _loggerMock.Object);

        var payload = """{"orderNumber":"ORD-123","status":"Created"}""";

        // Act
        var result = await service.ProcessOrderWebhookAsync("Trendyol", payload);

        // Assert
        result.Success.Should().BeTrue();
        result.EventType.Should().Be("OrderCreated");
        result.PlatformOrderId.Should().Be("ORD-123");

        webhookCapable.Verify(
            w => w.ProcessWebhookPayloadAsync(payload, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessGenericWebhook_UnknownType_StillSucceeds()
    {
        // Arrange
        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.PlatformCode).Returns("Trendyol");

        var webhookCapable = mockAdapter.As<IWebhookCapableAdapter>();
        webhookCapable
            .Setup(w => w.ProcessWebhookPayloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var adapters = new List<IIntegratorAdapter> { mockAdapter.Object };
        var service = new WebhookReceiverService(adapters, _loggerMock.Object);

        var payload = """{"data":"test"}""";

        // Act
        var result = await service.ProcessGenericWebhookAsync("Trendyol", "SomeUnknownEvent", payload);

        // Assert
        result.Success.Should().BeTrue();
        result.EventType.Should().Be("SomeUnknownEvent");
        result.Message.Should().NotBeNull();
        result.Message.Should().Contain("Bilinmeyen");
    }

    [Fact]
    public void HmacValidation_CorrectSignature()
    {
        // Arrange
        var payload = """{"orderNumber":"ORD-999"}""";
        var secret = "my-webhook-secret-key";

        // Compute valid HMAC-SHA256 Base64 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var validSignature = Convert.ToBase64String(hash);

        // Act & Assert — valid signature returns true
        WebhookEndpoints.ValidateHmacSignature(payload, validSignature, secret).Should().BeTrue();

        // Invalid signature returns false
        WebhookEndpoints.ValidateHmacSignature(payload, "invalid-sig", secret).Should().BeFalse();

        // Empty signature returns false
        WebhookEndpoints.ValidateHmacSignature(payload, "", secret).Should().BeFalse();

        // Empty secret returns false
        WebhookEndpoints.ValidateHmacSignature(payload, validSignature, "").Should().BeFalse();
    }
}
