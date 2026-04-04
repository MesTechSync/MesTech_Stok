using FluentAssertions;
using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OzonAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private OzonAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPlatformDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformDashboardDto
            {
                IsConnected = false,
                ProductCount = 0,
                OrderCount = 0,
                DailyRevenue = 0m,
                SyncStatus = "Bekliyor",
                LastSyncAt = null,
                RecentOrders = []
            });
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SyncPlatformCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto { IsSuccess = true, ItemsProcessed = 5 });
        return new OzonAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
        sut.IsConnected.Should().BeFalse();
        sut.ProductCount.Should().Be(0);
        sut.OrderCount.Should().Be(0);
        sut.DailyRevenue.Should().Be(0m);
        sut.SyncStatus.Should().Be("Bekliyor");
        sut.LastSyncTime.Should().Be("-");
        sut.RecentOrders.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingStates()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeTrue("no orders were added");
    }

    [Fact]
    public async Task LoadAsync_WhenNoOrders_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.RecentOrders.Should().BeEmpty();
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncCommand_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.SyncCommand.ExecuteAsync(null);

        // Assert — Sync calls LoadAsync afterwards which resets SyncStatus from dashboard query.
        sut.HasError.Should().BeFalse("SyncCommand should not throw");
        sut.IsLoading.Should().BeFalse();
        _mediatorMock.Verify(m => m.Send(
            It.IsAny<SyncPlatformCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldResetStateBetweenCalls()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsLoading.Should().BeFalse();
        sut.IsEmpty.Should().BeTrue();
    }
}
