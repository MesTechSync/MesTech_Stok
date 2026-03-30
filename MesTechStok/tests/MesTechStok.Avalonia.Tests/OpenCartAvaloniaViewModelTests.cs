using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OpenCartAvaloniaViewModelTests
{
    private static OpenCartAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var currentUserMock = new Mock<ICurrentUserService>();
        return new OpenCartAvaloniaViewModel(mediatorMock.Object, currentUserMock.Object);
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
