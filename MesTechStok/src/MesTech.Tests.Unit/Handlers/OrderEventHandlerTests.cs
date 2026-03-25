using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for order-related event handlers — verifies HandleAsync completes without exception.
/// OrderPlacedStockDeductionHandler already tested in ChainEventHandlerTests — SKIPPED.
/// </summary>
[Trait("Category", "Unit")]
public class OrderEventHandlerTests
{
    #region OrderCancelledEventHandler

    [Fact]
    public async Task OrderCancelledEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new OrderCancelledEventHandler(
            NullLogger<OrderCancelledEventHandler>.Instance);

        var evt = new OrderCancelledEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            PlatformCode: "Trendyol",
            PlatformOrderId: "TY-123456",
            Reason: "Customer request",
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OrderCancelledEventHandler_NullReason_CompletesSuccessfully()
    {
        var sut = new OrderCancelledEventHandler(
            NullLogger<OrderCancelledEventHandler>.Instance);

        var evt = new OrderCancelledEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            PlatformCode: "OpenCart",
            PlatformOrderId: "OC-999",
            Reason: null,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region OrderReceivedEventHandler

    [Fact]
    public async Task OrderReceivedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new OrderReceivedEventHandler(
            NullLogger<OrderReceivedEventHandler>.Instance);

        var evt = new OrderReceivedEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            PlatformCode: "Trendyol",
            PlatformOrderId: "TY-654321",
            TotalAmount: 1250.50m,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OrderReceivedEventHandler_ZeroAmount_CompletesSuccessfully()
    {
        var sut = new OrderReceivedEventHandler(
            NullLogger<OrderReceivedEventHandler>.Instance);

        var evt = new OrderReceivedEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            PlatformCode: "N11",
            PlatformOrderId: "N11-001",
            TotalAmount: 0m,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region OrderShippedEventHandler

    [Fact]
    public async Task OrderShippedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new OrderShippedEventHandler(
            NullLogger<OrderShippedEventHandler>.Instance);

        var evt = new OrderShippedEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            TrackingNumber: "YK123456789",
            CargoProvider: CargoProvider.YurticiKargo,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(CargoProvider.ArasKargo)]
    [InlineData(CargoProvider.SuratKargo)]
    [InlineData(CargoProvider.MngKargo)]
    public async Task OrderShippedEventHandler_VariousCargoProviders_CompletesSuccessfully(CargoProvider cargo)
    {
        var sut = new OrderShippedEventHandler(
            NullLogger<OrderShippedEventHandler>.Instance);

        var evt = new OrderShippedEvent(
            OrderId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            TrackingNumber: $"TRK-{cargo}",
            CargoProvider: cargo,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region StaleOrderDetectedEventHandler

    [Fact]
    public async Task StaleOrderDetectedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new StaleOrderDetectedEventHandler(
            NullLogger<StaleOrderDetectedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            orderId: Guid.NewGuid(),
            orderNumber: "ORD-2026-001",
            platform: PlatformType.Trendyol,
            elapsedSince: TimeSpan.FromHours(52),
            tenantId: Guid.NewGuid(),
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StaleOrderDetectedEventHandler_NullPlatform_CompletesSuccessfully()
    {
        var sut = new StaleOrderDetectedEventHandler(
            NullLogger<StaleOrderDetectedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            orderId: Guid.NewGuid(),
            orderNumber: "ORD-2026-002",
            platform: null,
            elapsedSince: TimeSpan.FromHours(96),
            tenantId: Guid.NewGuid(),
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region PaymentFailedEventHandler

    [Fact]
    public async Task PaymentFailedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new PaymentFailedEventHandler(
            NullLogger<PaymentFailedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            errorMessage: "Card declined",
            errorCode: "DECLINED",
            failureCount: 1,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PaymentFailedEventHandler_ThresholdReached_CompletesSuccessfully()
    {
        var sut = new PaymentFailedEventHandler(
            NullLogger<PaymentFailedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            errorMessage: "Insufficient funds",
            errorCode: "INSUFFICIENT_FUNDS",
            failureCount: 3,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PaymentFailedEventHandler_NullErrorFields_CompletesSuccessfully()
    {
        var sut = new PaymentFailedEventHandler(
            NullLogger<PaymentFailedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            errorMessage: null,
            errorCode: null,
            failureCount: 5,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion
}
