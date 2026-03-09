using FluentAssertions;
using MassTransit;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MesaEventPublisher unit testleri — DEV 5 Dalga 3.
/// Mock IPublishEndpoint ile publisher pipeline dogrulama.
/// Bridge handler testlerinden bagimsiz — publisher seviyesinde test.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga3")]
public class MesaEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly Mock<ILogger<MesaEventPublisher>> _loggerMock = new();

    private MesaEventPublisher CreateSut() =>
        new(_publishEndpointMock.Object, _loggerMock.Object);

    // ══════════════════════════════════════════════
    //  PublishProductCreatedAsync
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishProductCreated_ShouldCallPublishEndpoint_WithCorrectEvent()
    {
        var sut = CreateSut();
        var evt = new MesaProductCreatedEvent(
            Guid.NewGuid(), "SKU-PUB-001", "Publisher Test Urun",
            "Elektronik", 299.99m, null,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishProductCreatedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaProductCreatedEvent>(e =>
                    e.SKU == "SKU-PUB-001" &&
                    e.Name == "Publisher Test Urun" &&
                    e.Category == "Elektronik" &&
                    e.SalePrice == 299.99m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  PublishStockLowAsync
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishStockLow_ShouldCallPublishEndpoint_WithCorrectEvent()
    {
        var sut = CreateSut();
        var evt = new MesaStockLowEvent(
            Guid.NewGuid(), "SKU-LOW-PUB", 2, 15, 50,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishStockLowAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaStockLowEvent>(e =>
                    e.SKU == "SKU-LOW-PUB" &&
                    e.CurrentStock == 2 &&
                    e.MinimumStock == 15 &&
                    e.ReorderSuggestion == 50),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  PublishOrderReceivedAsync
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishOrderReceived_ShouldCallPublishEndpoint_WithCorrectEvent()
    {
        var sut = CreateSut();
        var evt = new MesaOrderReceivedEvent(
            Guid.NewGuid(), "Trendyol", "TY-2026-100",
            1250.00m, "+905551234567",
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishOrderReceivedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaOrderReceivedEvent>(e =>
                    e.PlatformCode == "Trendyol" &&
                    e.PlatformOrderId == "TY-2026-100" &&
                    e.TotalAmount == 1250.00m &&
                    e.CustomerPhone == "+905551234567"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  PublishPriceChangedAsync
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishPriceChanged_ShouldCallPublishEndpoint_WithCorrectEvent()
    {
        var sut = CreateSut();
        var evt = new MesaPriceChangedEvent(
            Guid.NewGuid(), "SKU-PRICE-PUB",
            150.00m, 129.90m,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishPriceChangedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaPriceChangedEvent>(e =>
                    e.SKU == "SKU-PRICE-PUB" &&
                    e.OldPrice == 150.00m &&
                    e.NewPrice == 129.90m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  CancellationToken forwarding
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishProductCreated_ShouldForwardCancellationToken()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var evt = new MesaProductCreatedEvent(
            Guid.NewGuid(), "SKU-CT-001", "CancellationToken Test",
            null, 10m, null,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishProductCreatedAsync(evt, token);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.IsAny<MesaProductCreatedEvent>(),
                token),
            Times.Once);
    }

    [Fact]
    public async Task PublishStockLow_ShouldForwardCancellationToken()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var evt = new MesaStockLowEvent(
            Guid.NewGuid(), "SKU-CT-002", 1, 5, null,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishStockLowAsync(evt, token);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.IsAny<MesaStockLowEvent>(),
                token),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  Exception propagation
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PublishProductCreated_WhenPublishEndpointThrows_ShouldPropagate()
    {
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<MesaProductCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection failed"));

        var sut = CreateSut();
        var evt = new MesaProductCreatedEvent(
            Guid.NewGuid(), "SKU-ERR-001", "Error Test",
            null, 50m, null,
            Guid.NewGuid(), DateTime.UtcNow);

        var act = async () => await sut.PublishProductCreatedAsync(evt);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RabbitMQ connection failed");
    }

    [Fact]
    public async Task PublishOrderReceived_WhenPublishEndpointThrows_ShouldPropagate()
    {
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<MesaOrderReceivedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Publish timeout"));

        var sut = CreateSut();
        var evt = new MesaOrderReceivedEvent(
            Guid.NewGuid(), "MesTech", "ORD-ERR-001",
            100m, null,
            Guid.NewGuid(), DateTime.UtcNow);

        var act = async () => await sut.PublishOrderReceivedAsync(evt);

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("Publish timeout");
    }

    // ══════════════════════════════════════════════
    //  Event DTO property verification
    // ══════════════════════════════════════════════

    [Fact]
    public void MesaProductCreatedEvent_ShouldHaveAllRequiredFields()
    {
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var imageUrls = new List<string> { "https://img.example.com/1.jpg", "https://img.example.com/2.jpg" };

        var evt = new MesaProductCreatedEvent(
            productId, "SKU-DTO-001", "DTO Test Urun",
            "Giyim", 199.99m, imageUrls,
            tenantId, occurredAt);

        evt.ProductId.Should().Be(productId);
        evt.SKU.Should().Be("SKU-DTO-001");
        evt.Name.Should().Be("DTO Test Urun");
        evt.Category.Should().Be("Giyim");
        evt.SalePrice.Should().Be(199.99m);
        evt.ImageUrls.Should().BeEquivalentTo(imageUrls);
        evt.TenantId.Should().Be(tenantId);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void MesaStockLowEvent_ShouldHaveAllRequiredFields()
    {
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var evt = new MesaStockLowEvent(
            productId, "SKU-DTO-002", 3, 10, 25,
            tenantId, occurredAt);

        evt.ProductId.Should().Be(productId);
        evt.SKU.Should().Be("SKU-DTO-002");
        evt.CurrentStock.Should().Be(3);
        evt.MinimumStock.Should().Be(10);
        evt.ReorderSuggestion.Should().Be(25);
        evt.TenantId.Should().Be(tenantId);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void MesaOrderReceivedEvent_ShouldHaveAllRequiredFields()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var evt = new MesaOrderReceivedEvent(
            orderId, "Trendyol", "TY-DTO-001",
            549.90m, "+905559876543",
            tenantId, occurredAt);

        evt.OrderId.Should().Be(orderId);
        evt.PlatformCode.Should().Be("Trendyol");
        evt.PlatformOrderId.Should().Be("TY-DTO-001");
        evt.TotalAmount.Should().Be(549.90m);
        evt.CustomerPhone.Should().Be("+905559876543");
        evt.TenantId.Should().Be(tenantId);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void MesaPriceChangedEvent_ShouldHaveAllRequiredFields()
    {
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var evt = new MesaPriceChangedEvent(
            productId, "SKU-DTO-003",
            200.00m, 179.90m,
            tenantId, occurredAt);

        evt.ProductId.Should().Be(productId);
        evt.SKU.Should().Be("SKU-DTO-003");
        evt.OldPrice.Should().Be(200.00m);
        evt.NewPrice.Should().Be(179.90m);
        evt.TenantId.Should().Be(tenantId);
        evt.OccurredAt.Should().Be(occurredAt);
    }
}
