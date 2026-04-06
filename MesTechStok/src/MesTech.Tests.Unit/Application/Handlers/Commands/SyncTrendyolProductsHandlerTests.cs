using FluentAssertions;
using MesTech.Application.Commands.SyncTrendyolProducts;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: SyncTrendyolProductsHandler testi — platform senkronizasyon.
/// P1: Trendyol entegrasyon hatası = satış kaybı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SyncTrendyolProductsHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    private SyncTrendyolProductsHandler CreateSut() => new(_orchestrator.Object);

    [Fact]
    public async Task Handle_ShouldDelegateToOrchestrator()
    {
        var expected = new SyncResultDto { ItemsProcessed = 100, IsSuccess = true };
        _orchestrator.Setup(o => o.SyncPlatformAsync("TRENDYOL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var cmd = new SyncTrendyolProductsCommand(Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _orchestrator.Setup(o => o.SyncPlatformAsync("TRENDYOL", cts.Token))
            .ReturnsAsync(new SyncResultDto());

        var cmd = new SyncTrendyolProductsCommand(Guid.NewGuid());
        await CreateSut().Handle(cmd, cts.Token);

        _orchestrator.Verify(o => o.SyncPlatformAsync("TRENDYOL", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_OrchestratorThrows_ShouldPropagateException()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("TRENDYOL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API timeout"));

        var cmd = new SyncTrendyolProductsCommand(Guid.NewGuid());

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*timeout*");
    }
}
