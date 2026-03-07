using FluentAssertions;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// SyncPlatform handler koruma testleri.
/// </summary>
public class SyncPlatformHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    [Fact]
    public async Task Handle_SpecificPlatform_ShouldCallSyncPlatformAsync()
    {
        var expected = new SyncResultDto { IsSuccess = true, PlatformCode = "trendyol" };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncPlatformHandler(_orchestrator.Object);
        var command = new SyncPlatformCommand("trendyol", SyncDirection.Push);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("trendyol");
        _orchestrator.Verify(o => o.SyncPlatformAsync("trendyol", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllPlatforms_ShouldCallSyncAllPlatformsAsync()
    {
        var expected = new SyncResultDto { IsSuccess = true, PlatformCode = "*" };
        _orchestrator
            .Setup(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncPlatformHandler(_orchestrator.Object);
        var command = new SyncPlatformCommand("*", SyncDirection.Push);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orchestrator.Verify(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FailedSync_ShouldReturnError()
    {
        var expected = new SyncResultDto
        {
            IsSuccess = false,
            PlatformCode = "opencart",
            ErrorMessage = "Connection refused"
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("opencart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncPlatformHandler(_orchestrator.Object);
        var command = new SyncPlatformCommand("opencart", SyncDirection.Pull);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection");
    }
}
