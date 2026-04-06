using FluentAssertions;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using MesTech.Application.Commands.SyncHepsiburadaProducts;
using MesTech.Application.Commands.SyncN11Products;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: Platform sync handler batch testleri — HB, N11, ÇS.
/// P1: Platform sync hatası = satış kaybı.
/// Aynı IIntegratorOrchestrator pattern — 3 handler tek dosyada.
/// </summary>

#region SyncHepsiburadaProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SyncHepsiburadaProductsHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    [Fact]
    public async Task Handle_ShouldDelegateToOrchestrator()
    {
        var expected = new SyncResultDto { IsSuccess = true, ItemsProcessed = 50 };
        _orchestrator.Setup(o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(new SyncHepsiburadaProductsCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_OrchestratorThrows_ShouldPropagate()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("HB API down"));

        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);
        var act = () => handler.Handle(new SyncHepsiburadaProductsCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

#endregion

#region SyncN11Products

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SyncN11ProductsHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    [Fact]
    public async Task Handle_ShouldCallN11Platform()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto { IsSuccess = true });

        var handler = new SyncN11ProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(new SyncN11ProductsCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orchestrator.Verify(o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region SyncCiceksepetiProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SyncCiceksepetiProductsHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    [Fact]
    public async Task Handle_ShouldCallCiceksepetiPlatform()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("CICEKSEPETI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto { IsSuccess = true, ItemsProcessed = 200 });

        var handler = new SyncCiceksepetiProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(new SyncCiceksepetiProductsCommand(Guid.NewGuid()), CancellationToken.None);

        result.ItemsProcessed.Should().Be(200);
    }
}

#endregion
