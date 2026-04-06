using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class OversellingAttemptedEventHandlerCommandTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ILogger<OversellingAttemptedEventHandler>> _logger = new();

    private OversellingAttemptedEventHandler CreateSut() => new(_productRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldLogCriticalLevel()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), "SKU-OVR", Guid.NewGuid(), 5, 10, "ORD-001", CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("OVERSELLING_ATTEMPTED")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteWithoutException()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), "SKU-TEST", Guid.NewGuid(), 0, 5, "ORD-002", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_NullOrderNumber_ShouldNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), "SKU-NULL", Guid.NewGuid(), 3, 8, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void HandleAsync_ShouldReturnCompletedTask()
    {
        var sut = CreateSut();

        var task = sut.HandleAsync(Guid.NewGuid(), "SKU-SYNC", Guid.NewGuid(), 2, 7, "ORD-003", CancellationToken.None);

        task.IsCompleted.Should().BeTrue();
    }
}
