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
public class SubscriptionNotificationHandlerCommandTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SubscriptionNotificationHandler>> _logger = new();

    private SubscriptionNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleCreatedAsync_ShouldCreateSubscriptionCreatedNotification()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleCreatedAsync(Guid.NewGuid(), tenantId, CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n =>
                n.TenantId == tenantId &&
                n.TemplateName == "SubscriptionCreated"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCancelledAsync_ShouldIncludeReasonInContent()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleCancelledAsync(Guid.NewGuid(), Guid.NewGuid(), "Too expensive", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Too expensive");
        captured.TemplateName.Should().Be("SubscriptionCancelled");
    }

    [Fact]
    public async Task HandleCancelledAsync_NullReason_ShouldShowDefaultText()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleCancelledAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("belirtilmedi");
    }

    [Fact]
    public async Task HandlePlanChangedAsync_ShouldIncludeNewPlanInContent()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandlePlanChangedAsync(Guid.NewGuid(), Guid.NewGuid(), "Enterprise", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Enterprise");
        captured.TemplateName.Should().Be("SubscriptionPlanChanged");
    }

    [Fact]
    public async Task HandlePlanChangedAsync_ShouldSaveChanges()
    {
        var sut = CreateSut();

        await sut.HandlePlanChangedAsync(Guid.NewGuid(), Guid.NewGuid(), "Pro", CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
