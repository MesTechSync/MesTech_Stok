using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using DomainNotificationChannel = MesTech.Domain.Enums.NotificationChannel;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// DEV5: StaleOrderNotificationHandler testi — Zincir 11 (gecikmiş sipariş bildirimi).
/// P0 event handler — müşteri memnuniyeti kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Chain", "Z11")]
public class StaleOrderNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<StaleOrderNotificationHandler>> _loggerMock = new();

    private StaleOrderNotificationHandler CreateSut() =>
        new(_notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCreateNotificationAndSave()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(orderId, tenantId, "ORD-STALE-001", PlatformType.Trendyol, TimeSpan.FromHours(48), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.TemplateName.Should().Be("StaleOrderAlert");
        captured.Channel.Should().Be(DomainNotificationChannel.Push);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeOrderNumberInContent()
    {
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-12345", PlatformType.Hepsiburada, TimeSpan.FromHours(72), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("ORD-12345");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeElapsedHoursInContent()
    {
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-TIME", PlatformType.N11, TimeSpan.FromHours(96), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("96");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludePlatformName()
    {
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-PLAT", PlatformType.Amazon, TimeSpan.FromHours(24), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Amazon");
    }

    [Fact]
    public async Task HandleAsync_NullPlatform_ShouldUseUnknown()
    {
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-NOPLAT", null, TimeSpan.FromHours(36), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Bilinmiyor");
    }

    [Fact]
    public async Task HandleAsync_RecipientShouldBeDashboard()
    {
        NotificationLog? captured = null;
        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-DASH", PlatformType.Trendyol, TimeSpan.FromHours(12), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Recipient.Should().Be("dashboard");
    }
}
