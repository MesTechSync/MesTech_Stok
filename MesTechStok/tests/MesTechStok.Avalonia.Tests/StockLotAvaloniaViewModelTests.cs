using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Features.Stock.Queries.GetStockLots;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockLotAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private StockLotAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetStockLotsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockLotDto>)new List<StockLotDto>
            {
                new() { Id = Guid.NewGuid(), LotNumber = "LOT-2026-001", ProductName = "Samsung Galaxy S24", ProductSku = "SKU-1001", Quantity = 100, RemainingQuantity = 80, UnitCost = 25000m, SupplierName = "Tedarikci A", WarehouseName = "Ana Depo", ReceivedAt = DateTime.Now.AddDays(-5) },
                new() { Id = Guid.NewGuid(), LotNumber = "LOT-2026-002", ProductName = "Apple MacBook Air M3", ProductSku = "SKU-1002", Quantity = 50, RemainingQuantity = 45, UnitCost = 45000m, SupplierName = "Tedarikci B", WarehouseName = "Yedek Depo", ReceivedAt = DateTime.Now.AddDays(-3) },
                new() { Id = Guid.NewGuid(), LotNumber = "LOT-2026-003", ProductName = "Sony WH-1000XM5", ProductSku = "SKU-1003", Quantity = 200, RemainingQuantity = 180, UnitCost = 8500m, SupplierName = "Tedarikci C", WarehouseName = "Iade Depo", ReceivedAt = DateTime.Now.AddDays(-1) },
                new() { Id = Guid.NewGuid(), LotNumber = "LOT-2026-004", ProductName = "Logitech MX Master", ProductSku = "SKU-1004", Quantity = 300, RemainingQuantity = 290, UnitCost = 3200m, SupplierName = "Tedarikci D", WarehouseName = "Ana Depo", ReceivedAt = DateTime.Now }
            });
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<ProductDto>.Create(
                new List<ProductDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Samsung Galaxy S24", SKU = "SKU-1001" }
                }, 1, 1, 10));
        return new StockLotAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
        sut.RecentLots.Should().HaveCount(4);
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
