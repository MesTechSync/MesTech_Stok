using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Webhooks;

/// <summary>
/// WebhookProcessor security + pipeline tests.
/// G493: unsigned reject, AllowUnsigned bypass, signature validation, event extraction.
/// </summary>
public class WebhookProcessorTests
{
    private readonly Mock<IWebhookSignatureValidator> _validatorMock;
    private readonly Mock<WebhookEventRouter> _routerMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Mock<ILogger<WebhookProcessor>> _loggerMock;
    private readonly Dictionary<string, string?> _configData;

    public WebhookProcessorTests()
    {
        _validatorMock = new Mock<IWebhookSignatureValidator>();
        _validatorMock.Setup(v => v.Platform).Returns("trendyol");

        _routerMock = new Mock<WebhookEventRouter>(
            Mock.Of<MediatR.IPublisher>(),
            Mock.Of<ILogger<WebhookEventRouter>>());

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());

        _loggerMock = new Mock<ILogger<WebhookProcessor>>();

        _configData = new Dictionary<string, string?>
        {
            ["Webhooks:Secrets:trendyol"] = "test-secret-key"
        };
    }

    private WebhookProcessor CreateProcessor(
        IEnumerable<IWebhookSignatureValidator>? validators = null,
        Dictionary<string, string?>? configOverrides = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configOverrides ?? _configData)
            .Build();

        return new WebhookProcessor(
            validators ?? new[] { _validatorMock.Object },
            _routerMock.Object,
            _tenantProviderMock.Object,
            config,
            _loggerMock.Object);
    }

    // ─── Signature Validation ───

    [Fact]
    public async Task ProcessAsync_ValidSignature_ReturnsSuccess()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), "valid-sig", "test-secret-key"))
            .Returns(true);

        _routerMock
            .Setup(r => r.RouteAsync("trendyol", "order.created", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("order.created");

        var processor = CreateProcessor();
        var payload = """{"event":"order.created","orderId":"123"}""";

        // Act
        var result = await processor.ProcessAsync("Trendyol", payload, "valid-sig", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.EventType.Should().Be("order.created");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_InvalidSignature_ReturnsFailed()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), "bad-sig", "test-secret-key"))
            .Returns(false);

        var processor = CreateProcessor();
        var payload = """{"event":"order.created"}""";

        // Act
        var result = await processor.ProcessAsync("Trendyol", payload, "bad-sig", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid webhook signature");
    }

    [Fact]
    public async Task ProcessAsync_MissingSignature_ValidatorRegistered_RejectsUnsigned()
    {
        // Arrange — validator exists for trendyol, no AllowUnsigned config
        var processor = CreateProcessor();
        var payload = """{"event":"order.created"}""";

        // Act — signature is null
        var result = await processor.ProcessAsync("Trendyol", payload, null, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("signature required but not provided");
    }

    [Fact]
    public async Task ProcessAsync_MissingSignature_AllowUnsignedTrue_Bypasses()
    {
        // Arrange — AllowUnsigned = true for trendyol
        var config = new Dictionary<string, string?>
        {
            ["Webhooks:Secrets:trendyol"] = "test-secret-key",
            ["Webhooks:AllowUnsigned:trendyol"] = "true"
        };

        _routerMock
            .Setup(r => r.RouteAsync("trendyol", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("order.created");

        var processor = CreateProcessor(configOverrides: config);
        var payload = """{"event":"order.created"}""";

        // Act
        var result = await processor.ProcessAsync("Trendyol", payload, null, CancellationToken.None);

        // Assert — should succeed despite no signature
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_NoValidatorForPlatform_SkipsSignatureCheck()
    {
        // Arrange — no validator registered for "unknown_platform"
        _routerMock
            .Setup(r => r.RouteAsync("unknown_platform", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("order.created");

        var processor = CreateProcessor();
        var payload = """{"event":"order.created"}""";

        // Act
        var result = await processor.ProcessAsync("unknown_platform", payload, null, CancellationToken.None);

        // Assert — should succeed, no signature validation needed
        result.Success.Should().BeTrue();
    }

    // ─── Platform Normalization ───

    [Theory]
    [InlineData("Trendyol", "trendyol")]
    [InlineData("TRENDYOL", "trendyol")]
    [InlineData("Shopify", "shopify")]
    public async Task ProcessAsync_NormalizesPlatformToLowerCase(string input, string expected)
    {
        // Arrange — no validator for these platforms (except trendyol)
        var emptyValidators = Array.Empty<IWebhookSignatureValidator>();

        _routerMock
            .Setup(r => r.RouteAsync(expected, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("order.created");

        var processor = CreateProcessor(validators: emptyValidators);
        var payload = """{"event":"order.created"}""";

        // Act
        var result = await processor.ProcessAsync(input, payload, null, CancellationToken.None);

        // Assert
        _routerMock.Verify(r => r.RouteAsync(expected, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Event Type Extraction ───

    [Theory]
    [InlineData("""{"event":"order.created"}""", "order.created")]
    [InlineData("""{"event_type":"stock.updated"}""", "stock.updated")]
    [InlineData("""{"eventType":"product.created"}""", "product.created")]
    [InlineData("""{"type":"return.created"}""", "return.created")]
    [InlineData("""{"topic":"invoice.created"}""", "invoice.created")]
    [InlineData("""{"webhook_topic":"orders/create"}""", "orders/create")]
    [InlineData("""{"resource":"products"}""", "products")]
    public async Task ProcessAsync_ExtractsEventType_FromVariousFields(string payload, string expectedEventType)
    {
        // Arrange
        var emptyValidators = Array.Empty<IWebhookSignatureValidator>();
        _routerMock
            .Setup(r => r.RouteAsync(It.IsAny<string>(), expectedEventType, payload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEventType);

        var processor = CreateProcessor(validators: emptyValidators);

        // Act
        var result = await processor.ProcessAsync("shopify", payload, null, CancellationToken.None);

        // Assert
        _routerMock.Verify(r => r.RouteAsync("shopify", expectedEventType, payload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_InvalidJson_ExtractsUnknownEventType()
    {
        // Arrange
        var emptyValidators = Array.Empty<IWebhookSignatureValidator>();
        _routerMock
            .Setup(r => r.RouteAsync("shopify", "unknown", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var processor = CreateProcessor(validators: emptyValidators);

        // Act
        var result = await processor.ProcessAsync("shopify", "not-json", null, CancellationToken.None);

        // Assert
        _routerMock.Verify(r => r.RouteAsync("shopify", "unknown", "not-json", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_EmptyJsonObject_ExtractsUnknownEventType()
    {
        // Arrange
        var emptyValidators = Array.Empty<IWebhookSignatureValidator>();
        _routerMock
            .Setup(r => r.RouteAsync("shopify", "unknown", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var processor = CreateProcessor(validators: emptyValidators);

        // Act
        var result = await processor.ProcessAsync("shopify", "{}", null, CancellationToken.None);

        // Assert
        _routerMock.Verify(r => r.RouteAsync("shopify", "unknown", "{}", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Error Handling ───

    [Fact]
    public async Task ProcessAsync_RouterThrows_ReturnsFailed()
    {
        // Arrange
        var emptyValidators = Array.Empty<IWebhookSignatureValidator>();
        _routerMock
            .Setup(r => r.RouteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Router exploded"));

        var processor = CreateProcessor(validators: emptyValidators);
        var payload = """{"event":"order.created"}""";

        // Act
        var result = await processor.ProcessAsync("trendyol", payload, null, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Router exploded");
    }
}
