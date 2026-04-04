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
public class CrmNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CrmNotificationHandler>> _loggerMock = new();

    private CrmNotificationHandler CreateHandler() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleDealWonAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();
        var dealId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleDealWonAsync(dealId, tenantId, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDealLostAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleDealLostAsync(Guid.NewGuid(), Guid.NewGuid(), "Price too high", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDealLostAsync_NullReason_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleDealLostAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDealStageChangedAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleDealStageChangedAsync(Guid.NewGuid(), Guid.NewGuid(), "Negotiation", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleLeadConvertedAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleLeadConvertedAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleLeadScoredAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleLeadScoredAsync(Guid.NewGuid(), Guid.NewGuid(), 85, CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCalendarEventCreatedAsync_ShouldCreateNotification()
    {
        var handler = CreateHandler();

        await handler.HandleCalendarEventCreatedAsync(Guid.NewGuid(), Guid.NewGuid(), "Team Meeting", CancellationToken.None);

        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
