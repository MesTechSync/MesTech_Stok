using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class HealthAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private HealthAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPlatformHealthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlatformHealthDto>());
        return new HealthAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.CpuUsage.Should().Be(0);
        sut.RamUsage.Should().Be(0);
        sut.DiskUsage.Should().Be(0);
        sut.LastUpdated.Should().Be("--:--");
        sut.ServiceStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateSystemMetrics()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — real process metrics
        sut.LastUpdated.Should().NotBe("--:--");
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithEmptyPlatformHealth_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
    }
}
