using FluentAssertions;
using MediatR;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;
using AppInventoryDto = MesTech.Application.DTOs.InventoryItemDto;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InventoryAvaloniaViewModelTests
{
    private static readonly List<AppInventoryDto> DemoItems =
    [
        new() { Barcode = "SKU-1001", ProductName = "Samsung Galaxy S24", Stock = 45, MinimumStock = 10, Price = 42999.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1002", ProductName = "Apple iPhone 15 Pro", Stock = 30, MinimumStock = 5, Price = 64999.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1003", ProductName = "Xiaomi Redmi Note 13", Stock = 120, MinimumStock = 20, Price = 12499.00m, Location = "Yedek Depo" },
        new() { Barcode = "SKU-1004", ProductName = "Huawei MateBook D16", Stock = 8, MinimumStock = 10, Price = 27999.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1005", ProductName = "Lenovo IdeaPad Slim 5", Stock = 15, MinimumStock = 5, Price = 22999.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1006", ProductName = "Sony WH-1000XM5", Stock = 60, MinimumStock = 10, Price = 9499.00m, Location = "Yedek Depo" },
        new() { Barcode = "SKU-1007", ProductName = "JBL Charge 5", Stock = 0, MinimumStock = 5, Price = 4299.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1008", ProductName = "Logitech MX Master 3S", Stock = 90, MinimumStock = 15, Price = 3299.00m, Location = "Ana Depo" },
        new() { Barcode = "SKU-1009", ProductName = "Anker PowerCore 26800", Stock = 200, MinimumStock = 30, Price = 1899.00m, Location = "Yedek Depo" },
        new() { Barcode = "SKU-1010", ProductName = "TP-Link Deco X50", Stock = 3, MinimumStock = 5, Price = 5999.00m, Location = "Ana Depo" }
    ];

    private static InventoryAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetInventoryPagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetInventoryPagedResult
            {
                Items = DemoItems,
                TotalItems = DemoItems.Count,
                CurrentPage = 1,
                PageSize = 500,
                TotalPages = 1
            });
        return new InventoryAvaloniaViewModel(mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
        sut.KpiOutOfStock.Should().Be(1); // SKU-1007 has Stock=0
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
