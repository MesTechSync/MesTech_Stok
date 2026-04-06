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
public class PriceChangedNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<PriceChangedNotificationHandler>> _loggerMock = new();

    private PriceChangedNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_PriceIncrease_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(Guid.NewGuid(), tenantId, "SKU-100", 100m, 150m, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PriceDecrease_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-200", 200m, 150m, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SamePrice_ShouldStillCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-EQ", 100m, 100m, CancellationToken.None);

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
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-X", 10m, 20m, CancellationToken.None);

        callOrder.Should().ContainInOrder("Add", "Save");
    }
}
