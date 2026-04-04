using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: SendNotificationHandler testi — bildirim gönderme.
/// P1: Bildirimler müşteri + operasyon iletişiminin temelidir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SendNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly Mock<IMesaEventMonitor> _monitor = new();
    private readonly Mock<ILogger<SendNotificationHandler>> _logger = new();

    private SendNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _publisher.Object, _monitor.Object, _logger.Object);

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateLogAndReturnId()
    {
        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "EMAIL", "user@test.com", "OrderConfirmed", "Sipariş onaylandı");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishToMesa()
    {
        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "PUSH", "dashboard", "Alert", "Stok düşük");

        await CreateSut().Handle(cmd, CancellationToken.None);

        _publisher.Verify(p => p.PublishAsync(
            It.IsAny<SendNotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishFails_ShouldStillReturnLogId()
    {
        _publisher.Setup(p => p.PublishAsync(It.IsAny<SendNotificationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RabbitMQ down"));

        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "TELEGRAM", "user", "Test", "Content");

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        // Should NOT throw — publish failure is caught, log is still returned
        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownChannel_ShouldDefaultToEmail()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var cmd = new SendNotificationCommand(
            Guid.NewGuid(), "PIGEON", "user", "Test", "Content");

        await CreateSut().Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Channel.Should().Be(MesTech.Domain.Enums.NotificationChannel.Email);
    }
}
