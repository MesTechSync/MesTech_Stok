using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// RÖNTGEN Zincir 11: StaleOrder → bildirim oluşturma.
/// Gecikmiş sipariş tespit edildiğinde NotificationLog kaydı oluşturulur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
[Trait("Group", "Chain11-StaleOrder")]
public class StaleOrderNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<StaleOrderNotificationHandler>> _logger = new();

    public StaleOrderNotificationHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private StaleOrderNotificationHandler CreateHandler() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_StaleOrder_CreatesNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            orderNumber: "ORD-20260330-ABC",
            platform: PlatformType.Trendyol,
            elapsedSince: TimeSpan.FromHours(48),
            ct: CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullPlatform_StillCreatesNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            orderNumber: "ORD-MANUAL-001",
            platform: null,
            elapsedSince: TimeSpan.FromHours(72),
            ct: CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
