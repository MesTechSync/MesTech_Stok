using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InventoryAvaloniaViewModelTests
{
    private static InventoryAvaloniaViewModel CreateSut() => new();

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
        sut.TotalCount.Should().Be(0);
        sut.AlarmCount.Should().Be(0);
        sut.CurrentPage.Should().Be(1);
        sut.Items.Should().BeEmpty();
        sut.WarehouseFilter.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItemsAndKpis()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.Items.Should().NotBeEmpty();
        sut.TotalCount.Should().BeGreaterThan(0);
        sut.KpiTotal.Should().BeGreaterThan(0);
        sut.KpiStockValue.Should().BeGreaterThan(0);
        sut.KpiOutOfStock.Should().BeGreaterOrEqualTo(0);
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.WarehouseFilter.Should().Contain("Tum Depolar");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKpiCorrectly()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — based on demo data: 10 items, 1 out of stock (Miktar=0), some critical
        sut.KpiTotal.Should().Be(10);
        sut.KpiOutOfStock.Should().Be(1); // SKU-1007 has Miktar=0
        sut.KpiCritical.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_SearchText_ShouldFilterItems()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Samsung";

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.First().Ad.Should().Contain("Samsung");
        sut.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_WarehouseFilter_ShouldReduceResults()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.TotalCount;

        // Act
        sut.SelectedWarehouse = "Yedek Depo";

        // Assert
        sut.TotalCount.Should().BeLessThan(totalBefore);
        sut.Items.Should().OnlyContain(i => i.Depo == "Yedek Depo");
    }
}
