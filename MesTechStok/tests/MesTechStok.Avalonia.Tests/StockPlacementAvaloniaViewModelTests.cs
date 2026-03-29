using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockPlacementAvaloniaViewModelTests
{
    private static StockPlacementAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new StockPlacementAvaloniaViewModel(mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    // ── 3-State: Default ──

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
        sut.SelectedWarehouse.Should().BeNull();
        sut.SelectedShelf.Should().BeNull();
        sut.Warehouses.Should().BeEmpty();
        sut.Shelves.Should().BeEmpty();
        sut.Items.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateWarehousesAndFilterItemsByDefaultWarehouse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Warehouses.Should().HaveCount(3);
        sut.SelectedWarehouse.Should().Be("Ana Depo");
        sut.Items.Should().NotBeEmpty();
        sut.Items.Should().OnlyContain(i => i.Depo == "Ana Depo");
        sut.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SelectedWarehouse_Change_ShouldUpdateShelvesAndItems()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var initialItemCount = sut.Items.Count;

        // Act
        sut.SelectedWarehouse = "Yedek Depo";

        // Assert
        sut.Items.Should().OnlyContain(i => i.Depo == "Yedek Depo");
        sut.Shelves.Should().NotBeEmpty();
        sut.SelectedShelf.Should().BeNull();
    }

    // ── 3-State: Filtered / Search ──

    [Fact]
    public async Task SearchText_ShouldFilterItemsBySkuOrName()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        // Default warehouse is "Ana Depo" — has multiple items

        // Act
        sut.SearchText = "Samsung";

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items[0].Ad.Should().Contain("Samsung");
    }

    [Fact]
    public async Task PlacementItemDto_StokDurum_ShouldReturnCorrectLevel()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Assert — verify stock status levels in demo data
        // SKU-1004: Miktar=0, MinimumStock=15 → TUKENDI
        // SKU-1002: Miktar=3, MinimumStock=5 → KRITIK
        var allItems = sut.Items.ToList();
        // Switch to all warehouses to check specific items
        sut.SearchText = string.Empty;

        // Check Ana Depo items
        var tukendi = sut.Items.FirstOrDefault(i => i.Sku == "SKU-1004");
        tukendi.Should().NotBeNull();
        tukendi!.StokDurum.Should().Be("TUKENDI");

        var kritik = sut.Items.FirstOrDefault(i => i.Sku == "SKU-1002");
        kritik.Should().NotBeNull();
        kritik!.StokDurum.Should().Be("KRITIK");
    }
}
