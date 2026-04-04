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
public class SyncErrorNotificationHandlerCommandTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SyncErrorNotificationHandler>> _logger = new();

    private SyncErrorNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateSyncErrorNotification()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(tenantId, "Trendyol", "ConnectionTimeout", "API down", CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n =>
                n.TenantId == tenantId &&
                n.TemplateName == "SyncError"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludePlatformInContent()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "Hepsiburada", "RateLimit", "Too many requests", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Hepsiburada");
        captured.Content.Should().Contain("RateLimit");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeErrorMessage()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "N11", "AuthFailed", "Invalid token", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRecipientToDashboard()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "Amazon", "ServerError", "500", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Recipient.Should().Be("dashboard");
    }
}
