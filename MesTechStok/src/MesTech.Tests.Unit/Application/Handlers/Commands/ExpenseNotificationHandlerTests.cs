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
public class ExpenseNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<ExpenseNotificationHandler>> _loggerMock = new();

    private ExpenseNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleApprovedAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleApprovedAsync(Guid.NewGuid(), tenantId, Guid.NewGuid(), CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaidAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandlePaidAsync(Guid.NewGuid(), tenantId, Guid.NewGuid(), CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleApprovedAsync_ShouldSaveChanges()
    {
        var handler = CreateHandler();

        await handler.HandleApprovedAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
