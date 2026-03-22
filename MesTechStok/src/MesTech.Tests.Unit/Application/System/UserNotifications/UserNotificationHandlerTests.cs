using FluentAssertions;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.System.UserNotifications;

[Trait("Category", "Unit")]
[Trait("Feature", "UserNotifications")]
public class UserNotificationHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    private static UserNotification MakeNotification(string title = "Test")
        => UserNotification.Create(_tenantId, _userId, title, "Test message", NotificationCategory.System);

    // ── MarkUserNotificationReadHandler ──

    [Fact]
    public async Task MarkNotificationRead_ValidNotification_ShouldReturnTrue()
    {
        // Arrange
        var notification = MakeNotification();
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new MarkUserNotificationReadHandler(mockRepo.Object, mockUow.Object);
        var command = new MarkUserNotificationReadCommand(_tenantId, notification.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        mockRepo.Verify(r => r.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkNotificationRead_NotFound_ShouldReturnFalse()
    {
        // Arrange
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        var handler = new MarkUserNotificationReadHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());

        // Act
        var result = await handler.Handle(
            new MarkUserNotificationReadCommand(_tenantId, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkNotificationRead_WrongTenant_ShouldReturnFalse()
    {
        // Arrange
        var notification = MakeNotification();
        var wrongTenantId = Guid.NewGuid();
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = new MarkUserNotificationReadHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());

        // Act
        var result = await handler.Handle(
            new MarkUserNotificationReadCommand(wrongTenantId, notification.Id), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkNotificationRead_NullRequest_ShouldThrow()
    {
        var handler = new MarkUserNotificationReadHandler(
            Mock.Of<IUserNotificationRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── MarkAllUserNotificationsReadHandler ──

    [Fact]
    public async Task MarkAllRead_WithUnread_ShouldMarkAllAndReturnCount()
    {
        // Arrange
        var n1 = MakeNotification("N1");
        var n2 = MakeNotification("N2");
        var unread = new List<UserNotification> { n1, n2 }.AsReadOnly();

        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetUnreadByUserAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unread);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new MarkAllUserNotificationsReadHandler(mockRepo.Object, mockUow.Object);

        // Act
        var result = await handler.Handle(
            new MarkAllUserNotificationsReadCommand(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(2);
        n1.IsRead.Should().BeTrue();
        n2.IsRead.Should().BeTrue();
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAllRead_NoUnread_ShouldReturnZeroAndSkipSave()
    {
        // Arrange
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetUnreadByUserAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserNotification>().AsReadOnly());
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new MarkAllUserNotificationsReadHandler(mockRepo.Object, mockUow.Object);

        // Act
        var result = await handler.Handle(
            new MarkAllUserNotificationsReadCommand(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(0);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAllRead_NullRequest_ShouldThrow()
    {
        var handler = new MarkAllUserNotificationsReadHandler(
            Mock.Of<IUserNotificationRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetUnreadNotificationCountHandler ──

    [Fact]
    public async Task GetUnreadCount_ShouldReturnCount()
    {
        // Arrange
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetUnreadCountAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var handler = new GetUnreadNotificationCountHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadNotificationCountQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetUnreadCount_ZeroUnread_ShouldReturnZero()
    {
        // Arrange
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetUnreadCountAsync(_tenantId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new GetUnreadNotificationCountHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadNotificationCountQuery(_tenantId, _userId), CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadCount_NullRequest_ShouldThrow()
    {
        var handler = new GetUnreadNotificationCountHandler(Mock.Of<IUserNotificationRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetUserNotificationsHandler ──

    [Fact]
    public async Task GetUserNotifications_WithItems_ShouldReturnMappedResult()
    {
        // Arrange
        var n1 = MakeNotification("Stock Alert");
        var notifications = new List<UserNotification> { n1 }.AsReadOnly();

        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(_tenantId, _userId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notifications, 1));

        var handler = new GetUserNotificationsHandler(mockRepo.Object);
        var query = new GetUserNotificationsQuery(_tenantId, _userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Stock Alert");
        result.Items[0].Category.Should().Be("System");
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetUserNotifications_UnreadOnly_ShouldPassFilter()
    {
        // Arrange
        var mockRepo = new Mock<IUserNotificationRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(_tenantId, _userId, 1, 20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification>().AsReadOnly(), 0));

        var handler = new GetUserNotificationsHandler(mockRepo.Object);
        var query = new GetUserNotificationsQuery(_tenantId, _userId, UnreadOnly: true);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        mockRepo.Verify(r => r.GetPagedAsync(
            _tenantId, _userId, 1, 20, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_NullRequest_ShouldThrow()
    {
        var handler = new GetUserNotificationsHandler(Mock.Of<IUserNotificationRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
