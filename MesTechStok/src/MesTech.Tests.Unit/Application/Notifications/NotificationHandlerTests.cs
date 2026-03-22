using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Tests.Unit.Application.Notifications;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: Notification Handler Tests
// ════════════════════════════════════════════════════════

#region MarkNotificationReadHandler

[Trait("Category", "Unit")]
public class MarkNotificationReadHandlerTests
{
    private readonly Mock<INotificationLogRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private MarkNotificationReadHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingDeliveredNotification_ShouldMarkAsReadAndReturnTrue()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = NotificationLog.Create(
            _tenantId, DomainChannel.Email, "test@test.com", "OrderConfirm", "Order confirmed");
        notification.MarkAsSent();
        notification.MarkAsDelivered();

        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = CreateHandler();
        var command = new MarkNotificationReadCommand(_tenantId, notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFoundNotification_ShouldReturnFalse()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);

        var handler = CreateHandler();
        var command = new MarkNotificationReadCommand(_tenantId, notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DifferentTenantId_ShouldReturnFalse()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var notification = NotificationLog.Create(
            otherTenantId, DomainChannel.Email, "test@test.com", "Template", "Content");

        _repo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var handler = CreateHandler();
        var command = new MarkNotificationReadCommand(_tenantId, notificationId);

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

#region SendNotificationHandler

[Trait("Category", "Unit")]
public class SendNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notificationLogRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly Mock<IMesaEventMonitor> _monitor = new();
    private readonly Mock<ILogger<SendNotificationHandler>> _logger = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private SendNotificationHandler CreateHandler() =>
        new(_notificationLogRepo.Object, _uow.Object, _messagePublisher.Object, _monitor.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateLogAndPublish()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendNotificationCommand(
            _tenantId, "EMAIL", "test@test.com", "OrderConfirm", "Your order is confirmed");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _notificationLogRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messagePublisher.Verify(p => p.PublishAsync(It.IsAny<SendNotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishFails_ShouldStillReturnLogId()
    {
        // Arrange — publish throws but handler should catch and still return log ID
        _messagePublisher.Setup(p => p.PublishAsync(It.IsAny<SendNotificationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ down"));

        var handler = CreateHandler();
        var command = new SendNotificationCommand(
            _tenantId, "WHATSAPP", "+90555", "StockAlert", "Low stock warning");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty(); // Log was created despite publish failure
        _notificationLogRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownChannel_ShouldDefaultToEmail()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendNotificationCommand(
            _tenantId, "UNKNOWN_CHANNEL", "user@test.com", "Template", "Content");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — should not throw, defaults to Email
        result.Should().NotBeEmpty();
        _notificationLogRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.Channel == DomainChannel.Email),
            It.IsAny<CancellationToken>()), Times.Once);
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

#region UpdateNotificationSettingsHandler

[Trait("Category", "Unit")]
public class UpdateNotificationSettingsHandlerTests
{
    private readonly Mock<INotificationSettingRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<UpdateNotificationSettingsHandler>> _logger = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private UpdateNotificationSettingsHandler CreateHandler() =>
        new(_repo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ExistingSetting_ShouldUpdateAndReturnId()
    {
        // Arrange
        var existing = new NotificationSetting
        {
            TenantId = _tenantId,
            UserId = _userId,
            Channel = DomainChannel.Email,
            IsEnabled = false
        };

        _repo.Setup(r => r.GetByUserAndChannelAsync(_userId, DomainChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var command = new UpdateNotificationSettingsCommand(
            _tenantId, _userId, DomainChannel.Email, "test@test.com", true,
            true, true, 5, true, true, true, false, true, true, true, true,
            null, null, "tr", false, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existing.Id);
        existing.IsEnabled.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewSetting_ShouldCreateAndReturnId()
    {
        // Arrange
        _repo.Setup(r => r.GetByUserAndChannelAsync(_userId, DomainChannel.Telegram, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationSetting?)null);

        var handler = CreateHandler();
        var command = new UpdateNotificationSettingsCommand(
            _tenantId, _userId, DomainChannel.Telegram, "@testchat", true,
            true, true, 5, true, true, true, false, true, true, true, true,
            null, null, "tr", false, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.IsAny<NotificationSetting>(), It.IsAny<CancellationToken>()), Times.Once);
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

#region GetNotificationSettingsHandler

[Trait("Category", "Unit")]
public class GetNotificationSettingsHandlerTests
{
    private readonly Mock<INotificationSettingRepository> _repo = new();

    private GetNotificationSettingsHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_NoSettings_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationSetting>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationSettingsQuery(Guid.NewGuid(), userId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSettings_ShouldMapToDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = new NotificationSetting
        {
            UserId = userId,
            Channel = DomainChannel.Email,
            IsEnabled = true,
            NotifyOnLowStock = true,
            LowStockThreshold = 10,
            PreferredLanguage = "tr",
        };

        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationSetting> { setting }.AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationSettingsQuery(Guid.NewGuid(), userId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Channel.Should().Be("Email");
        result[0].IsEnabled.Should().BeTrue();
        result[0].LowStockThreshold.Should().Be(10);
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

#region GetNotificationsHandler

[Trait("Category", "Unit")]
public class GetNotificationsHandlerTests
{
    private readonly Mock<INotificationLogRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetNotificationsHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyListWithZeroCount()
    {
        // Arrange
        _repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NotificationLog>().AsReadOnly(), 0));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationsQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNotifications_ShouldMapToDto()
    {
        // Arrange
        var log = NotificationLog.Create(
            _tenantId, DomainChannel.Telegram, "@chat123", "StockAlert", "Stock low!");

        _repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NotificationLog> { log }.AsReadOnly(), 1));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationsQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Channel.Should().Be("Telegram");
        result.Items[0].TemplateName.Should().Be("StockAlert");
    }

    [Fact]
    public async Task Handle_UnreadOnlyFilter_ShouldForwardToRepository()
    {
        // Arrange
        _repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NotificationLog>().AsReadOnly(), 0));

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetNotificationsQuery(_tenantId, 1, 10, true), CancellationToken.None);

        // Assert
        _repo.Verify(r => r.GetPagedAsync(_tenantId, 1, 10, true, It.IsAny<CancellationToken>()), Times.Once);
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
