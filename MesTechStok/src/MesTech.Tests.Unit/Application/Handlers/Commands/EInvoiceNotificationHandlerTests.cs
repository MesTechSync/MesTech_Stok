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
public class EInvoiceNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<EInvoiceNotificationHandler>> _loggerMock = new();

    private EInvoiceNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleCreatedAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();
        var tenantId = Guid.NewGuid();

        await handler.HandleCreatedAsync(Guid.NewGuid(), tenantId, "ETTN-001", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSentAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleSentAsync(Guid.NewGuid(), Guid.NewGuid(), "ETTN-002", "REF-123", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSentAsync_NullProviderRef_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleSentAsync(Guid.NewGuid(), Guid.NewGuid(), "ETTN-003", null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCancelledAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleCancelledAsync(Guid.NewGuid(), Guid.NewGuid(), "ETTN-004", "Customer request", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
