using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class LowStockNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<LowStockNotificationHandler>> _loggerMock = new();

    private LowStockNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(Guid.NewGuid(), tenantId, "SKU-001", 3, 10, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroStock_ShouldStillCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-ZERO", 0, 5, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAddBeforeSave()
    {
        var callOrder = new List<string>();
        _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Add"))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Save"))
            .Returns(Task.FromResult(0));

        var handler = CreateHandler();
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-X", 2, 10, CancellationToken.None);

        callOrder.Should().ContainInOrder("Add", "Save");
    }
}
