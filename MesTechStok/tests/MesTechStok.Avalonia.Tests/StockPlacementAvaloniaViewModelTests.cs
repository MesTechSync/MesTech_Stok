using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockPlacements;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockPlacementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private StockPlacementAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetStockPlacementsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockPlacementDto>().AsReadOnly());
        return new StockPlacementAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — empty mock data
        sut.Items.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    // ── 3-State: Filtered / Search ──

    [Fact]
    public async Task SearchText_WhenNoData_ShouldRemainEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Samsung";

        // Assert — no data to filter
        sut.Items.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void PlacementItemDto_StokDurum_ShouldReturnCorrectLevel()
    {
        // Assert — verify stock status levels via DTO directly
        var tukendi = new PlacementItemDto { Sku = "SKU-1004", Miktar = 0, MinimumStock = 15 };
        tukendi.StokDurum.Should().Be("TUKENDI");

        var kritik = new PlacementItemDto { Sku = "SKU-1002", Miktar = 3, MinimumStock = 5 };
        kritik.StokDurum.Should().Be("KRITIK");
    }
}
