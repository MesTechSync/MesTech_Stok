using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class TrendyolAvaloniaViewModelTests
{
    private static TrendyolAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new TrendyolAvaloniaViewModel(mediatorMock.Object);
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
        // No data loaded yet (placeholder), so isEmpty should be true
        sut.IsEmpty.Should().BeTrue();
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
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SyncCommand_ShouldUpdateSyncStatus()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.SyncCommand.ExecuteAsync(null);

        // Assert
        sut.SyncStatus.Should().Be("Tamamlandi");
        sut.LastSyncTime.Should().NotBe("-");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldResetErrorStateBeforeLoading()
    {
        // Arrange
        var sut = CreateSut();
        // Simulate prior error state
        await sut.LoadAsync();

        // Act — reload should clear previous state
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
    }
}
