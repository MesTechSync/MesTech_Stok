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
[Trait("Category", "Unit")]
public class SyncPlatformHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    private SyncPlatformHandler CreateHandler() => new(_orchestrator.Object);

    [Fact]
    public async Task Handle_SpecificPlatform_ShouldCallSyncPlatformAsync()
    {
        var expected = new SyncResultDto { IsSuccess = true, PlatformCode = "trendyol" };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
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

        var handler = CreateHandler();
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

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("opencart", SyncDirection.Pull);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection");
    }

    // ── DEV5 Dalga 1: Expanded Handler Tests ──

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_OrchestratorThrowsException_ShouldPropagate()
    {
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("failing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Adapter not registered"));

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("failing", SyncDirection.Push);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public async Task Handle_EmptyPlatformCode_ShouldCallSyncPlatformAsync()
    {
        var expected = new SyncResultDto { IsSuccess = false, PlatformCode = "", ErrorMessage = "Unknown platform" };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("", SyncDirection.Pull);

        var result = await handler.Handle(command, CancellationToken.None);

        // Empty string != "*", so it should call SyncPlatformAsync, not SyncAllPlatformsAsync
        _orchestrator.Verify(o => o.SyncPlatformAsync("", It.IsAny<CancellationToken>()), Times.Once);
        _orchestrator.Verify(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SyncResult_ShouldPreserveItemCounts()
    {
        var expected = new SyncResultDto
        {
            IsSuccess = true,
            PlatformCode = "trendyol",
            ItemsProcessed = 150,
            ItemsFailed = 3,
            Warnings = new List<string> { "SKU-001: price mismatch" }
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("trendyol", SyncDirection.Push);

        var result = await handler.Handle(command, CancellationToken.None);

        result.ItemsProcessed.Should().Be(150);
        result.ItemsFailed.Should().Be(3);
        result.Warnings.Should().ContainSingle().Which.Should().Contain("SKU-001");
    }

    [Fact]
    public async Task Handle_CancellationToken_ShouldBeForwardedToOrchestrator()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expected = new SyncResultDto { IsSuccess = true, PlatformCode = "hepsiburada" };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("hepsiburada", token))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("hepsiburada", SyncDirection.Pull);

        var result = await handler.Handle(command, token);

        result.IsSuccess.Should().BeTrue();
        // Verify the exact token was forwarded, not just any CancellationToken
        _orchestrator.Verify(o => o.SyncPlatformAsync("hepsiburada", token), Times.Once);
    }
}
