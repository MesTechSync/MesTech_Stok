using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockLotAvaloniaViewModelTests
{
    private static StockLotAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new StockLotAvaloniaViewModel(mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    // ── 3-State: Loading ──

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
        sut.SaveStatus.Should().BeEmpty();
        sut.LotNumber.Should().BeEmpty();
        sut.Quantity.Should().Be(0);
        sut.UnitCost.Should().Be(0);
        sut.SelectedProduct.Should().BeNull();
        sut.ProductSuggestions.Should().BeEmpty();
        sut.Suppliers.Should().BeEmpty();
        sut.Warehouses.Should().BeEmpty();
        sut.RecentLots.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateSuppliersAndWarehousesAndRecentLots()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Suppliers.Should().HaveCount(4);
        sut.Warehouses.Should().HaveCount(3);
        sut.RecentLots.Should().HaveCount(3);
        sut.RecentLots[0].LotNo.Should().Be("LOT-2026-001");
    }

    // ── 3-State: Data / Loaded ──

    [Fact]
    public void ProductSearchText_ShortInput_ShouldClearSuggestions()
    {
        // Arrange
        var sut = CreateSut();

        // Act — search text shorter than 2 chars
        sut.ProductSearchText = "S";

        // Assert
        sut.ProductSuggestions.Should().BeEmpty();
    }

    [Fact]
    public void ProductSearchText_ValidInput_ShouldPopulateSuggestions()
    {
        // Arrange
        var sut = CreateSut();

        // Act — search text >= 2 chars matching "Samsung"
        sut.ProductSearchText = "Samsung";

        // Assert
        sut.ProductSuggestions.Should().NotBeEmpty();
        sut.ProductSuggestions.Should().Contain(p => p.Ad.Contains("Samsung"));
    }

    // ── 3-State: Validation ──

    [Fact]
    public async Task SaveLotCommand_WithoutSelectedProduct_ShouldSetSaveStatus()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.SelectedProduct = null;
        sut.LotNumber = "LOT-TEST-001";
        sut.Quantity = 10;
        sut.SelectedWarehouse = "Ana Depo";

        // Act
        await sut.SaveLotCommand.ExecuteAsync(null);

        // Assert
        sut.SaveStatus.Should().Be("Urun secilmedi.");
    }
}
