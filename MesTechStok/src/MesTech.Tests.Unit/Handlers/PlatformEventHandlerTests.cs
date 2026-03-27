using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for platform-related event handlers — verifies HandleAsync completes without exception.
/// </summary>
[Trait("Category", "Unit")]
public class PlatformEventHandlerTests
{
    #region PlatformMessageReceivedEventHandler

    [Fact]
    public async Task PlatformMessageReceivedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new PlatformMessageReceivedEventHandler(
            NullLogger<PlatformMessageReceivedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            messageId: Guid.NewGuid(),
            platform: PlatformType.Trendyol,
            senderName: "Musteri A",
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(PlatformType.OpenCart)]
    [InlineData(PlatformType.Trendyol)]
    public async Task PlatformMessageReceivedEventHandler_VariousPlatforms_CompletesSuccessfully(PlatformType platform)
    {
        var sut = new PlatformMessageReceivedEventHandler(
            NullLogger<PlatformMessageReceivedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            messageId: Guid.NewGuid(),
            platform: platform,
            senderName: $"Sender_{platform}",
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region PlatformNotificationFailedEventHandler

    [Fact]
    public async Task PlatformNotificationFailedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new PlatformNotificationFailedEventHandler(
            NullLogger<PlatformNotificationFailedEventHandler>.Instance);

        var evt = new PlatformNotificationFailedEvent
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            PlatformCode = "Trendyol",
            TrackingNumber = "YK987654321",
            CargoProvider = CargoProvider.YurticiKargo,
            ErrorMessage = "Connection timeout",
            RetryCount = 2
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PlatformNotificationFailedEventHandler_HighRetryCount_CompletesSuccessfully()
    {
        var sut = new PlatformNotificationFailedEventHandler(
            NullLogger<PlatformNotificationFailedEventHandler>.Instance);

        var evt = new PlatformNotificationFailedEvent
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            PlatformCode = "Hepsiburada",
            TrackingNumber = "AR123456",
            CargoProvider = CargoProvider.ArasKargo,
            ErrorMessage = "API rate limit exceeded",
            RetryCount = 10
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ProductCreatedEventHandler

    [Fact]
    public async Task ProductCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ProductCreatedEventHandler(
            NullLogger<ProductCreatedEventHandler>.Instance);

        var evt = new ProductCreatedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SKU: "SKU-TEST-001",
            Name: "Test Product",
            SalePrice: 199.99m,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ProductUpdatedEventHandler

    [Fact]
    public async Task ProductUpdatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ProductUpdatedEventHandler(
            NullLogger<ProductUpdatedEventHandler>.Instance);

        var evt = new ProductUpdatedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SKU: "SKU-UPD-001",
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ReconciliationCompletedEventHandler

    [Fact]
    public async Task ReconciliationCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ReconciliationCompletedEventHandler(
            NullLogger<ReconciliationCompletedEventHandler>.Instance);

        var evt = new ReconciliationCompletedEvent
        {
            MatchId = Guid.NewGuid(),
            SettlementBatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            FinalStatus = ReconciliationStatus.AutoMatched,
            Confidence = 0.95m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(ReconciliationStatus.AutoMatched)]
    [InlineData(ReconciliationStatus.NeedsReview)]
    [InlineData(ReconciliationStatus.ManualMatch)]
    [InlineData(ReconciliationStatus.Rejected)]
    public async Task ReconciliationCompletedEventHandler_AllStatuses_CompletesSuccessfully(ReconciliationStatus status)
    {
        var sut = new ReconciliationCompletedEventHandler(
            NullLogger<ReconciliationCompletedEventHandler>.Instance);

        var evt = new ReconciliationCompletedEvent
        {
            MatchId = Guid.NewGuid(),
            SettlementBatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            FinalStatus = status,
            Confidence = 0.75m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ReconciliationMatchedEventHandler

    [Fact]
    public async Task ReconciliationMatchedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ReconciliationMatchedEventHandler(
            NullLogger<ReconciliationMatchedEventHandler>.Instance);

        var evt = new ReconciliationMatchedEvent
        {
            ReconciliationMatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            SettlementBatchId = Guid.NewGuid(),
            Confidence = 0.88m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReconciliationMatchedEventHandler_NullableIds_CompletesSuccessfully()
    {
        var sut = new ReconciliationMatchedEventHandler(
            NullLogger<ReconciliationMatchedEventHandler>.Instance);

        var evt = new ReconciliationMatchedEvent
        {
            ReconciliationMatchId = Guid.NewGuid(),
            BankTransactionId = null,
            SettlementBatchId = null,
            Confidence = 0.50m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SyncErrorOccurredEventHandler

    [Fact]
    public async Task SyncErrorOccurredEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new SyncErrorOccurredEventHandler(
            NullLogger<SyncErrorOccurredEventHandler>.Instance);

        var evt = new SyncErrorOccurredEvent(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            ErrorType: "API_TIMEOUT",
            Message: "Connection timed out after 30s",
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SyncRequestedEventHandler

    [Fact]
    public async Task SyncRequestedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new SyncRequestedEventHandler(
            NullLogger<SyncRequestedEventHandler>.Instance);

        var evt = new SyncRequestedEvent(
            TenantId: Guid.NewGuid(),
            PlatformCode: "Trendyol",
            Direction: SyncDirection.Pull,
            EntityType: "Product",
            EntityId: Guid.NewGuid().ToString(),
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(SyncDirection.Push)]
    [InlineData(SyncDirection.Pull)]
    [InlineData(SyncDirection.Bidirectional)]
    public async Task SyncRequestedEventHandler_AllDirections_CompletesSuccessfully(SyncDirection direction)
    {
        var sut = new SyncRequestedEventHandler(
            NullLogger<SyncRequestedEventHandler>.Instance);

        var evt = new SyncRequestedEvent(
            TenantId: Guid.NewGuid(),
            PlatformCode: "OpenCart",
            Direction: direction,
            EntityType: "Order",
            EntityId: null,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SupplierFeedSyncedEventHandler

    [Fact]
    public async Task SupplierFeedSyncedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new SupplierFeedSyncedEventHandler(
            NullLogger<SupplierFeedSyncedEventHandler>.Instance);

        var evt = new SupplierFeedSyncedEvent(
            SupplierFeedId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SupplierId: Guid.NewGuid(),
            TotalProducts: 500,
            UpdatedProducts: 45,
            DeactivatedProducts: 3,
            Status: FeedSyncStatus.Completed,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(FeedSyncStatus.Completed)]
    [InlineData(FeedSyncStatus.PartiallyCompleted)]
    [InlineData(FeedSyncStatus.Failed)]
    public async Task SupplierFeedSyncedEventHandler_VariousStatuses_CompletesSuccessfully(FeedSyncStatus status)
    {
        var sut = new SupplierFeedSyncedEventHandler(
            NullLogger<SupplierFeedSyncedEventHandler>.Instance);

        var evt = new SupplierFeedSyncedEvent(
            SupplierFeedId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            SupplierId: Guid.NewGuid(),
            TotalProducts: 100,
            UpdatedProducts: 10,
            DeactivatedProducts: 0,
            Status: status,
            OccurredAt: DateTime.UtcNow);

        Func<Task> act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion
}
