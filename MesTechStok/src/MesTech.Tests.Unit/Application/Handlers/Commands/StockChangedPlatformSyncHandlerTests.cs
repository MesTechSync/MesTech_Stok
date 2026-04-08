using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class StockChangedPlatformSyncHandlerCommandTests
{
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _logger = new();

    private StockChangedPlatformSyncHandler CreateSut() => new(_logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCompleteWithoutException()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-SYNC",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void HandleAsync_ShouldReturnCompletedTask()
    {
        var sut = CreateSut();

        var task = sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-FAST",
            100, 95, StockMovementType.Sale, CancellationToken.None);

        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ZeroNewQuantity_ShouldLogWarning()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-ZERO",
            5, 0, StockMovementType.Sale, CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("STOK SIFIR")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonZeroQuantity_ShouldNotLogWarning()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-NORM",
            50, 10, StockMovementType.Sale, CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
