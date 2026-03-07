using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Webhooks;
using Microsoft.Extensions.Logging;
using Moq;
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
        Assert.True(result.Success);
        Assert.Equal("OrderCreated", result.EventType);
        Assert.Equal("ORD-123", result.PlatformOrderId);

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
        Assert.True(result.Success);
        Assert.Equal("SomeUnknownEvent", result.EventType);
        Assert.NotNull(result.Message);
        Assert.Contains("Bilinmeyen", result.Message);
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
        Assert.True(WebhookEndpoints.ValidateHmacSignature(payload, validSignature, secret));

        // Invalid signature returns false
        Assert.False(WebhookEndpoints.ValidateHmacSignature(payload, "invalid-sig", secret));

        // Empty signature returns false
        Assert.False(WebhookEndpoints.ValidateHmacSignature(payload, "", secret));

        // Empty secret returns false
        Assert.False(WebhookEndpoints.ValidateHmacSignature(payload, validSignature, ""));
    }
}
