using FluentAssertions;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.System;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: System UserNotification Handler Tests
// ════════════════════════════════════════════════════════

#region MarkAllUserNotificationsReadHandler

[Trait("Category", "Unit")]
public class MarkAllUserNotificationsReadHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private MarkAllUserNotificationsReadHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_NoUnreadNotifications_ShouldReturnZero()
    {
        // Arrange
        _repo.Setup(r => r.GetUnreadByUserAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserNotification>().AsReadOnly());

        var handler = CreateHandler();
        var command = new MarkAllUserNotificationsReadCommand(_tenantId, _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnreadNotifications_ShouldMarkAllAndReturnCount()
    {
        // Arrange
        var n1 = UserNotification.Create(_tenantId, _userId, "Title 1", "Message 1", NotificationCategory.Order);
        var n2 = UserNotification.Create(_tenantId, _userId, "Title 2", "Message 2", NotificationCategory.Stock);

        _repo.Setup(r => r.GetUnreadByUserAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserNotification> { n1, n2 }.AsReadOnly());

        var handler = CreateHandler();
        var command = new MarkAllUserNotificationsReadCommand(_tenantId, _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(2);
        n1.IsRead.Should().BeTrue();
        n2.IsRead.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region MarkUserNotificationReadHandler

[Trait("Category", "Unit")]
public class MarkUserNotificationReadHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private MarkUserNotificationReadHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingNotification_ShouldMarkAsReadAndReturnTrue()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = UserNotification.Create(
            _tenantId, Guid.NewGuid(), "Test", "Message", NotificationCategory.System);

        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = CreateHandler();
        var command = new MarkUserNotificationReadCommand(_tenantId, notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFoundNotification_ShouldReturnFalse()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        var handler = CreateHandler();
        var command = new MarkUserNotificationReadCommand(_tenantId, notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WrongTenantId_ShouldReturnFalse()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var notification = UserNotification.Create(
            otherTenantId, Guid.NewGuid(), "Test", "Message", NotificationCategory.System);

        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = CreateHandler();
        var command = new MarkUserNotificationReadCommand(_tenantId, notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetUnreadNotificationCountHandler

[Trait("Category", "Unit")]
public class GetUnreadNotificationCountHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private GetUnreadNotificationCountHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_ShouldReturnCountFromRepository()
    {
        // Arrange
        _repo.Setup(r => r.GetUnreadCountAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetUnreadNotificationCountQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task Handle_ZeroUnread_ShouldReturnZero()
    {
        // Arrange
        _repo.Setup(r => r.GetUnreadCountAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetUnreadNotificationCountQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetUserNotificationsHandler

[Trait("Category", "Unit")]
public class GetUserNotificationsHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private GetUserNotificationsHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyListWithZeroCount()
    {
        // Arrange
        _repo.Setup(r => r.GetPagedAsync(_tenantId, _userId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification>().AsReadOnly(), 0));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetUserNotificationsQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNotifications_ShouldMapToDto()
    {
        // Arrange
        var notification = UserNotification.Create(
            _tenantId, _userId, "Stock Alert", "Product X is low", NotificationCategory.Stock, "/stock/alerts");

        _repo.Setup(r => r.GetPagedAsync(_tenantId, _userId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification> { notification }.AsReadOnly(), 1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetUserNotificationsQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Stock Alert");
        result.Items[0].Category.Should().Be("Stock");
        result.Items[0].ActionUrl.Should().Be("/stock/alerts");
        result.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnreadOnlyFilter_ShouldForwardToRepository()
    {
        // Arrange
        _repo.Setup(r => r.GetPagedAsync(_tenantId, _userId, 2, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification>().AsReadOnly(), 0));

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetUserNotificationsQuery(_tenantId, _userId, 2, 10, true), CancellationToken.None);

        // Assert
        _repo.Verify(r => r.GetPagedAsync(_tenantId, _userId, 2, 10, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
