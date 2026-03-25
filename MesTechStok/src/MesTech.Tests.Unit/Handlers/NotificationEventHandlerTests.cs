using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Notification & UserNotification Handler Tests
// SendNotification, MarkNotificationRead, MarkAllUserNotificationsRead,
// MarkUserNotificationRead, GetUnreadNotificationCount, GetUserNotifications
// ═══════════════════════════════════════════════════════════════

#region SendNotificationHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class SendNotificationHandlerTests2
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IMessagePublisher> _publisherMock = new();
    private readonly Mock<IMesaEventMonitor> _monitorMock = new();
    private readonly SendNotificationHandler _sut;

    public SendNotificationHandlerTests2()
    {
        _sut = new SendNotificationHandler(
            _notifRepoMock.Object, _uowMock.Object,
            _publisherMock.Object, _monitorMock.Object,
            Mock.Of<ILogger<SendNotificationHandler>>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLogAndPublishes()
    {
        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "EMAIL", "user@test.com", "welcome", "Hello!");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PublishFails_StillReturnsLogId()
    {
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ down"));

        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "TELEGRAM", "+905551234567", "alert", "Server down!");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region MarkNotificationReadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class MarkNotificationReadHandlerTests2
{
    private readonly Mock<INotificationLogRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MarkNotificationReadHandler _sut;

    public MarkNotificationReadHandlerTests2()
    {
        _sut = new MarkNotificationReadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);

        var cmd = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region MarkAllUserNotificationsReadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class MarkAllUserNotificationsReadHandlerTests2
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MarkAllUserNotificationsReadHandler _sut;

    public MarkAllUserNotificationsReadHandlerTests2()
    {
        _sut = new MarkAllUserNotificationsReadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NoUnread_ReturnsZeroAndSkipsSave()
    {
        _repoMock.Setup(r => r.GetUnreadByUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserNotification>().AsReadOnly());

        var cmd = new MarkAllUserNotificationsReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(0);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region MarkUserNotificationReadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class MarkUserNotificationReadHandlerTests2
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MarkUserNotificationReadHandler _sut;

    public MarkUserNotificationReadHandlerTests2()
    {
        _sut = new MarkUserNotificationReadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        var cmd = new MarkUserNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetUnreadNotificationCountHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class GetUnreadNotificationCountHandlerTests2
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly GetUnreadNotificationCountHandler _sut;

    public GetUnreadNotificationCountHandlerTests2()
    {
        _sut = new GetUnreadNotificationCountHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsCount()
    {
        _repoMock.Setup(r => r.GetUnreadCountAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetUnreadNotificationCountQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetUserNotificationsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
public class GetUserNotificationsHandlerTests2
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly GetUserNotificationsHandler _sut;

    public GetUserNotificationsHandlerTests2()
    {
        _sut = new GetUserNotificationsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResult()
    {
        var items = new List<UserNotification>().AsReadOnly();
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 0));

        var query = new GetUserNotificationsQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion
