using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockAlertAvaloniaViewModelTests
{
    private static StockAlertAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new StockAlertAvaloniaViewModel(mediatorMock.Object);
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
        sut.CurrentFilter.Should().Be("All");
        sut.FilteredAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingAndPopulateAlerts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.FilteredAlerts.Should().NotBeEmpty();
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.AlertSummary.Should().Contain("tukendi");
        sut.AlertSummary.Should().Contain("kritik");
    }

    [Fact]
    public async Task LoadAsync_FilterByOutOfStock_ShouldShowOnlyOutOfStockAlerts()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.FilteredAlerts.Count;

        // Act
        sut.FilterOutOfStockCommand.Execute(null);

        // Assert
        sut.FilteredAlerts.Should().NotBeEmpty();
        sut.FilteredAlerts.Count.Should().BeLessThan(totalBefore);
        sut.FilteredAlerts.Should().OnlyContain(a => a.Level == "OutOfStock");
        sut.CurrentFilter.Should().Be("OutOfStock");
    }

    [Fact]
    public async Task LoadAsync_AlertSummary_ShouldContainCorrectCounts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — based on demo data: 3 OutOfStock, 2 Critical, 3 Low
        sut.AlertSummary.Should().Be("3 tukendi | 2 kritik | 3 dusuk");
        sut.FilteredAlerts.Should().HaveCount(8);
    }

    [Fact]
    public async Task LoadAsync_FilterAll_ShouldShowAllAlerts()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Filter to OutOfStock first
        sut.FilterOutOfStockCommand.Execute(null);
        var filteredCount = sut.FilteredAlerts.Count;

        // Act — reset to All
        sut.FilterAllCommand.Execute(null);

        // Assert
        sut.FilteredAlerts.Count.Should().BeGreaterThan(filteredCount);
        sut.CurrentFilter.Should().Be("All");
    }
}
