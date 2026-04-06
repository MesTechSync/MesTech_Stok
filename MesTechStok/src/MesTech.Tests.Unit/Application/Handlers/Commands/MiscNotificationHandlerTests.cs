using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class MiscNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<MiscNotificationHandler>> _logger = new();

    private MiscNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateNotificationAndSave()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(tenantId, "BuyboxLostEvent", "Buybox lost for SKU-123", CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n =>
                n.TenantId == tenantId &&
                n.TemplateName == "BuyboxLostEvent"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRecipientToDashboard()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "TaskCompletedEvent", "Task done", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Recipient.Should().Be("dashboard");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetChannelToPush()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "DocumentUploadedEvent", "Doc uploaded", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Channel.Should().Be(MesTech.Domain.Enums.NotificationChannel.Push);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassContentToNotification()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        var content = "Supplier feed synced successfully at 14:30";
        await sut.HandleAsync(Guid.NewGuid(), "SupplierFeedSyncedEvent", content, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Be(content);
    }
}
