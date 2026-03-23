using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WelcomeAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private WelcomeAvaloniaViewModel CreateSut() => new(_mediatorMock.Object);

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
        sut.WelcomeText.Should().Be("Entegrator Stok Yonetim Sistemi");
        sut.TotalProducts.Should().Be("0");
        sut.TotalOrders.Should().Be("0");
        sut.ActivePlatforms.Should().Be("0");
        sut.RecentActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateKPIValues()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.TotalProducts.Should().Be("3,284");
        sut.TotalOrders.Should().Be("156");
        sut.ActivePlatforms.Should().Be("5");
        sut.WelcomeText.Should().Be("Entegrator Stok Yonetim Sistemi");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate5RecentActivities()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.RecentActivities.Should().HaveCount(5);
        sut.RecentActivities[0].Description.Should().Contain("Trendyol");
        sut.RecentActivities.Should().AllSatisfy(a =>
        {
            a.Description.Should().NotBeNullOrEmpty();
            a.TimeAgo.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task LoadAsync_3StateTransition_ShouldEndInSuccessState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 3-state check
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_RepeatedCalls_ShouldClearPreviousData()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.RecentActivities.Should().HaveCount(5);

        // Act — second load should clear and repopulate
        await sut.LoadAsync();

        // Assert — should not double up
        sut.RecentActivities.Should().HaveCount(5);
        sut.TotalProducts.Should().Be("3,284");
    }
}
