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
public class ProductLifecycleNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<ProductLifecycleNotificationHandler>> _loggerMock = new();

    private ProductLifecycleNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleUpdatedAsync_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleUpdatedAsync(Guid.NewGuid(), tenantId, "SKU-UPD", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleActivatedAsync_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleActivatedAsync(Guid.NewGuid(), tenantId, "SKU-ACT", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDeactivatedAsync_ShouldCreateNotificationAndSave()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleDeactivatedAsync(Guid.NewGuid(), tenantId, "SKU-DEACT", "Out of season", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDeactivatedAsync_EmptyReason_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleDeactivatedAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-X", "", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
