using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// DailySummaryBridgeHandler + SyncErrorBridgeHandler unit testleri — DEV3 Dalga 5 gorev 7.
/// Mock IMesaEventPublisher + Mock ITenantProvider ile pipeline dogrulama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class DailySummarySyncErrorBridgeTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Mock<ITenantProvider> CreateTenantProvider()
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return mock;
    }

    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    // ══════════════════════════════════════════════
    //  DailySummaryBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task DailySummary_Handler_PublishesMesaEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new DailySummaryBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<DailySummaryBridgeHandler>>().Object);

        var domainEvent = new DailySummaryGeneratedEvent(
            Guid.NewGuid(), new DateTime(2026, 3, 10), 42, 15890.50m, 3, 18, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<DailySummaryGeneratedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishDailySummaryAsync(
                It.Is<MesaDailySummaryEvent>(e =>
                    e.Date == new DateTime(2026, 3, 10) &&
                    e.OrderCount == 42 &&
                    e.Revenue == 15890.50m &&
                    e.StockAlerts == 3 &&
                    e.InvoiceCount == 18 &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DailySummary_Handler_RecordsMonitor()
    {
        var monitorMock = CreateMonitor();
        var handler = new DailySummaryBridgeHandler(
            new Mock<IMesaEventPublisher>().Object,
            monitorMock.Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<DailySummaryBridgeHandler>>().Object);

        var domainEvent = new DailySummaryGeneratedEvent(
            Guid.NewGuid(), DateTime.Today, 10, 5000m, 1, 5, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<DailySummaryGeneratedEvent>(domainEvent),
            CancellationToken.None);

        monitorMock.Verify(
            x => x.RecordPublish("daily.summary"),
            Times.Once);
    }

    [Fact]
    public async Task DailySummary_Handler_IncludesTenantId()
    {
        var tenantId = Guid.NewGuid();
        var tenantMock = new Mock<ITenantProvider>();
        tenantMock.Setup(x => x.GetCurrentTenantId()).Returns(tenantId);

        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new DailySummaryBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            tenantMock.Object,
            new Mock<ILogger<DailySummaryBridgeHandler>>().Object);

        var domainEvent = new DailySummaryGeneratedEvent(
            Guid.NewGuid(), DateTime.Today, 5, 2500m, 0, 3, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<DailySummaryGeneratedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishDailySummaryAsync(
                It.Is<MesaDailySummaryEvent>(e => e.TenantId == tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  SyncErrorBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SyncError_Handler_PublishesMesaEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new SyncErrorBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<SyncErrorBridgeHandler>>().Object);

        var domainEvent = new SyncErrorOccurredEvent(
            Guid.NewGuid(), "Trendyol", "RateLimitExceeded", "429 Too Many Requests", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<SyncErrorOccurredEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishSyncErrorAsync(
                It.Is<MesaSyncErrorEvent>(e =>
                    e.Platform == "Trendyol" &&
                    e.ErrorType == "RateLimitExceeded" &&
                    e.Message == "429 Too Many Requests" &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncError_Handler_RecordsMonitor()
    {
        var monitorMock = CreateMonitor();
        var handler = new SyncErrorBridgeHandler(
            new Mock<IMesaEventPublisher>().Object,
            monitorMock.Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<SyncErrorBridgeHandler>>().Object);

        var domainEvent = new SyncErrorOccurredEvent(
            Guid.NewGuid(), "HepsiBurada", "Timeout", "Connection timed out", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<SyncErrorOccurredEvent>(domainEvent),
            CancellationToken.None);

        monitorMock.Verify(
            x => x.RecordPublish("sync.error"),
            Times.Once);
    }

    [Fact]
    public async Task SyncError_Handler_LogsWarning()
    {
        var loggerMock = new Mock<ILogger<SyncErrorBridgeHandler>>();
        var handler = new SyncErrorBridgeHandler(
            new Mock<IMesaEventPublisher>().Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            loggerMock.Object);

        var domainEvent = new SyncErrorOccurredEvent(
            Guid.NewGuid(), "Ciceksepeti", "AuthFailed", "401 Unauthorized", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<SyncErrorOccurredEvent>(domainEvent),
            CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SyncError yakalandi")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
