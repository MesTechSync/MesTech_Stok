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
public class PriceLossDetectedEventHandlerCommandTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<PriceLossDetectedEventHandler>> _logger = new();

    private PriceLossDetectedEventHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateNotificationWithPriceLossAlertTemplate()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(Guid.NewGuid(), tenantId, "SKU-LOSS", 100m, 85m, 15m, CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n =>
                n.TenantId == tenantId &&
                n.TemplateName == "PriceLossAlert"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeSkuInContent()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-EXPENSIVE", 200m, 150m, 50m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("SKU-EXPENSIVE");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeLossAmountInContent()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-X", 100m, 70m, 30m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("ZARAR");
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveChanges()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-SAVE", 50m, 40m, 10m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
