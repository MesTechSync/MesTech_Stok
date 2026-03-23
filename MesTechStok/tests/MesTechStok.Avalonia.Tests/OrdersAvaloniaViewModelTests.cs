using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OrdersAvaloniaViewModelTests
{
    private static OrdersAvaloniaViewModel CreateSut() => new();

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
        sut.SearchText.Should().BeEmpty();
        sut.SelectedStatus.Should().Be("Tumu");
        sut.TotalCount.Should().Be(0);
        sut.Orders.Should().BeEmpty();
        sut.Statuses.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingDuringExecution()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — after completion loading must be false
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateOrders()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Orders.Should().NotBeEmpty();
        sut.TotalCount.Should().BeGreaterThan(0);
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Orders.First().OrderNo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoadAsync_FilterByStatus_ShouldReduceResults()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.TotalCount;

        // Act
        sut.SelectedStatus = "Yeni";

        // Assert
        sut.TotalCount.Should().BeLessThan(totalBefore);
        sut.Orders.Should().OnlyContain(o => o.Status == "Yeni");
    }

    [Fact]
    public async Task LoadAsync_SearchText_ShouldFilterByCustomerOrOrderNo()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — search for a known customer fragment
        sut.SearchText = "Ahmet";

        // Assert
        sut.Orders.Should().HaveCount(1);
        sut.Orders.First().Customer.Should().Contain("Ahmet");
    }
}
