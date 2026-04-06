using FluentAssertions;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using MesTech.Application.Commands.SyncHepsiburadaProducts;
using MesTech.Application.Commands.SyncN11Products;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Commands;

/// <summary>
/// DEV5: Sync handler tests for Ciceksepeti, N11, Hepsiburada platforms.
/// All three handlers delegate to IIntegratorOrchestrator.SyncPlatformAsync.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Sync")]
public class SyncPlatformProductsHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    #region SyncCiceksepetiProductsHandler

    [Fact]
    public async Task Ciceksepeti_Handle_ValidCommand_ShouldCallAdapterAndSave()
    {
        var expected = new SyncResultDto
        {
            IsSuccess = true,
            PlatformCode = "CICEKSEPETI",
            ItemsProcessed = 75
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("CICEKSEPETI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncCiceksepetiProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncCiceksepetiProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("CICEKSEPETI");
        _orchestrator.Verify(
            o => o.SyncPlatformAsync("CICEKSEPETI", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Ciceksepeti_Handle_AdapterNotFound_ShouldReturnError()
    {
        var errorResult = new SyncResultDto
        {
            IsSuccess = false,
            PlatformCode = "CICEKSEPETI",
            ErrorMessage = "Adapter not found for platform CICEKSEPETI"
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("CICEKSEPETI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var handler = new SyncCiceksepetiProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncCiceksepetiProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Adapter not found");
    }

    [Fact]
    public async Task Ciceksepeti_Handle_AdapterThrows_ShouldHandleGracefully()
    {
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("CICEKSEPETI", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Ciceksepeti API timeout"));

        var handler = new SyncCiceksepetiProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(
            new SyncCiceksepetiProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*timeout*");
    }

    [Fact]
    public void Ciceksepeti_Constructor_NullOrchestrator_ShouldThrow()
    {
        var act = () => new SyncCiceksepetiProductsHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("orchestrator");
    }

    #endregion

    #region SyncN11ProductsHandler

    [Fact]
    public async Task N11_Handle_ValidCommand_ShouldCallAdapterAndSave()
    {
        var expected = new SyncResultDto
        {
            IsSuccess = true,
            PlatformCode = "N11",
            ItemsProcessed = 200
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncN11ProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncN11ProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("N11");
        _orchestrator.Verify(
            o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task N11_Handle_AdapterNotFound_ShouldReturnError()
    {
        var errorResult = new SyncResultDto
        {
            IsSuccess = false,
            PlatformCode = "N11",
            ErrorMessage = "Adapter not found for platform N11"
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var handler = new SyncN11ProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncN11ProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Adapter not found");
    }

    [Fact]
    public async Task N11_Handle_AdapterThrows_ShouldHandleGracefully()
    {
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("N11", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("N11 API connection refused"));

        var handler = new SyncN11ProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(
            new SyncN11ProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*connection refused*");
    }

    [Fact]
    public void N11_Constructor_NullOrchestrator_ShouldThrow()
    {
        var act = () => new SyncN11ProductsHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("orchestrator");
    }

    #endregion

    #region SyncHepsiburadaProductsHandler

    [Fact]
    public async Task Hepsiburada_Handle_ValidCommand_ShouldCallAdapterAndSave()
    {
        var expected = new SyncResultDto
        {
            IsSuccess = true,
            PlatformCode = "HEPSIBURADA",
            ItemsProcessed = 150
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncHepsiburadaProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("HEPSIBURADA");
        _orchestrator.Verify(
            o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Hepsiburada_Handle_AdapterNotFound_ShouldReturnError()
    {
        var errorResult = new SyncResultDto
        {
            IsSuccess = false,
            PlatformCode = "HEPSIBURADA",
            ErrorMessage = "Adapter not found for platform HEPSIBURADA"
        };
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);
        var result = await handler.Handle(
            new SyncHepsiburadaProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Adapter not found");
    }

    [Fact]
    public async Task Hepsiburada_Handle_AdapterThrows_ShouldHandleGracefully()
    {
        _orchestrator
            .Setup(o => o.SyncPlatformAsync("HEPSIBURADA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Hepsiburada API 503 Service Unavailable"));

        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(
            new SyncHepsiburadaProductsCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*503*");
    }

    [Fact]
    public void Hepsiburada_Constructor_NullOrchestrator_ShouldThrow()
    {
        var act = () => new SyncHepsiburadaProductsHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("orchestrator");
    }

    #endregion

    #region Null Command Guard

    [Fact]
    public async Task Ciceksepeti_Handle_NullCommand_ShouldThrow()
    {
        var handler = new SyncCiceksepetiProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task N11_Handle_NullCommand_ShouldThrow()
    {
        var handler = new SyncN11ProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Hepsiburada_Handle_NullCommand_ShouldThrow()
    {
        var handler = new SyncHepsiburadaProductsHandler(_orchestrator.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
