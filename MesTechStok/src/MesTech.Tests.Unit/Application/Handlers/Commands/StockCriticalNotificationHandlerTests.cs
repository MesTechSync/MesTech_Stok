using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class StockCriticalNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<StockCriticalNotificationHandler>> _loggerMock = new();

    private StockCriticalNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_CriticalLevel_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(
            Guid.NewGuid(), tenantId, "SKU-CRIT", "Critical Product",
            2, 10, StockAlertLevel.Critical,
            null, null,
            CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OutOfStock_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-OOS", "Out Product",
            0, 5, StockAlertLevel.OutOfStock,
            Guid.NewGuid(), "Depo-1",
            CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LowLevel_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-LOW", "Low Product",
            8, 10, StockAlertLevel.Low,
            null, null,
            CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullWarehouse_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-X", "Product X",
            1, 5, StockAlertLevel.Critical,
            null, null,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithWarehouse_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-WH", "Warehouse Product",
            0, 10, StockAlertLevel.OutOfStock,
            Guid.NewGuid(), "Istanbul Depo",
            CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
