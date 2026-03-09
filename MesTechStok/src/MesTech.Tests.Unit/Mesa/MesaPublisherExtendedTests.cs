using FluentAssertions;
using MassTransit;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MesaEventPublisher extended tests — remaining 6 publish methods.
/// Covers: InvoiceGenerated, InvoiceCancelled, ReturnCreated, ReturnResolved,
/// BuyboxLost, SupplierFeedSynced.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga4")]
public class MesaPublisherExtendedTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly Mock<ILogger<MesaEventPublisher>> _loggerMock = new();

    private MesaEventPublisher CreateSut() =>
        new(_publishEndpointMock.Object, _loggerMock.Object);

    // ════ 1. PublishInvoiceGeneratedAsync ════

    [Fact]
    public async Task PublishInvoiceGenerated_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaInvoiceGeneratedEvent(
            Guid.NewGuid(), Guid.NewGuid(),
            "GIB-2026-001", "EFatura",
            "Ali Yilmaz", "ali@example.com", "+905551234567",
            1500.00m, "https://example.com/inv.pdf",
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishInvoiceGeneratedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaInvoiceGeneratedEvent>(e =>
                    e.InvoiceNumber == "GIB-2026-001" &&
                    e.InvoiceType == "EFatura" &&
                    e.GrandTotal == 1500.00m &&
                    e.PdfUrl == "https://example.com/inv.pdf"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════ 2. PublishInvoiceCancelledAsync ════

    [Fact]
    public async Task PublishInvoiceCancelled_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaInvoiceCancelledEvent(
            Guid.NewGuid(), "FTR-2026-002",
            "Musteri iade talebi",
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishInvoiceCancelledAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaInvoiceCancelledEvent>(e =>
                    e.InvoiceNumber == "FTR-2026-002" &&
                    e.CancelReason == "Musteri iade talebi"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════ 3. PublishReturnCreatedAsync ════

    [Fact]
    public async Task PublishReturnCreated_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaReturnCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(),
            "Trendyol", "Ali Yilmaz", "+905551234567",
            "DefectiveProduct", 2, 299.80m,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishReturnCreatedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaReturnCreatedEvent>(e =>
                    e.PlatformCode == "Trendyol" &&
                    e.ReturnReason == "DefectiveProduct" &&
                    e.ItemCount == 2 &&
                    e.TotalAmount == 299.80m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════ 4. PublishReturnResolvedAsync ════

    [Fact]
    public async Task PublishReturnResolved_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaReturnResolvedEvent(
            Guid.NewGuid(), Guid.NewGuid(),
            "Refunded", 149.90m,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishReturnResolvedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaReturnResolvedEvent>(e =>
                    e.Resolution == "Refunded" &&
                    e.RefundAmount == 149.90m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════ 5. PublishBuyboxLostAsync ════

    [Fact]
    public async Task PublishBuyboxLost_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaBuyboxLostEvent(
            Guid.NewGuid(), "SKU-BB-PUB",
            149.90m, 139.90m, "TeknoMarket", 10.00m,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishBuyboxLostAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaBuyboxLostEvent>(e =>
                    e.SKU == "SKU-BB-PUB" &&
                    e.CompetitorPrice == 139.90m &&
                    e.CompetitorName == "TeknoMarket" &&
                    e.PriceDifference == 10.00m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════ 6. PublishSupplierFeedSyncedAsync ════

    [Fact]
    public async Task PublishSupplierFeedSynced_ShouldCallPublishEndpoint()
    {
        var sut = CreateSut();
        var evt = new MesaSupplierFeedSyncedEvent(
            Guid.NewGuid(), "TestTedarikci",
            "Xml", 250, 15, 45, 3,
            Guid.NewGuid(), DateTime.UtcNow);

        await sut.PublishSupplierFeedSyncedAsync(evt);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<MesaSupplierFeedSyncedEvent>(e =>
                    e.SupplierName == "TestTedarikci" &&
                    e.FeedFormat == "Xml" &&
                    e.ProductsTotal == 250 &&
                    e.ProductsNew == 15 &&
                    e.ProductsUpdated == 45 &&
                    e.ProductsDeactivated == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
