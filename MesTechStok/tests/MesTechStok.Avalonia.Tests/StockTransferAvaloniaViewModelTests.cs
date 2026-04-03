using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockTransferAvaloniaViewModelTests
{
    private static StockTransferAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetStockTransfersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockTransferItemDto>)new List<StockTransferItemDto>
            {
                new() { Id = Guid.NewGuid(), ProductName = "Samsung Galaxy S24", SKU = "SKU-1001", Quantity = 10, MovementType = "Ana Depo", Reference = "Yedek Depo", MovementDate = DateTime.Now.AddDays(-1) },
                new() { Id = Guid.NewGuid(), ProductName = "Apple MacBook Air M3", SKU = "SKU-1002", Quantity = 5, MovementType = "Yedek Depo", Reference = "Ana Depo", MovementDate = DateTime.Now.AddDays(-2) },
                new() { Id = Guid.NewGuid(), ProductName = "Sony WH-1000XM5", SKU = "SKU-1003", Quantity = 20, MovementType = "Ana Depo", Reference = "Iade Depo", MovementDate = DateTime.Now.AddDays(-3) }
            });
        return new StockTransferAvaloniaViewModel(mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
        sut.TransferStatus.Should().BeEmpty();
        sut.SelectedSourceWarehouse.Should().BeNull();
        sut.SelectedTargetWarehouse.Should().BeNull();
        sut.SelectedProduct.Should().BeNull();
        sut.TransferQuantity.Should().Be(0);
        sut.SourceStock.Should().Be(0);
        sut.RemainingStock.Should().Be(0);
        sut.Warehouses.Should().BeEmpty();
        sut.SourceProducts.Should().BeEmpty();
        sut.TransferHistory.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateWarehousesAndTransferHistory()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Warehouses.Should().HaveCount(3);
        sut.TransferHistory.Should().HaveCount(3);
    }

    [Fact]
    public async Task SelectedSourceWarehouse_ShouldLoadSourceProducts()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SelectedSourceWarehouse = "Ana Depo";

        // Assert
        sut.SourceProducts.Should().NotBeEmpty();
        sut.SourceProducts.Should().OnlyContain(p => p.Depo == "Ana Depo");
        sut.SelectedProduct.Should().BeNull("selecting warehouse resets product");
        sut.SourceStock.Should().Be(0);
    }

    // ── 3-State: Validation ──

    [Fact]
    public async Task TransferCommand_SameSourceAndTarget_ShouldSetStatus()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.SelectedSourceWarehouse = "Ana Depo";
        sut.SelectedTargetWarehouse = "Ana Depo";
        sut.SelectedProduct = sut.SourceProducts.First();
        sut.TransferQuantity = 5;

        // Act
        await sut.TransferCommand.ExecuteAsync(null);

        // Assert
        sut.TransferStatus.Should().Be("Kaynak ve hedef depo ayni olamaz.");
    }

    [Fact]
    public async Task TransferCommand_QuantityExceedsStock_ShouldSetStatus()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.SelectedSourceWarehouse = "Ana Depo";
        sut.SelectedTargetWarehouse = "Yedek Depo";
        var product = sut.SourceProducts.First();
        sut.SelectedProduct = product;
        sut.TransferQuantity = product.Miktar + 100;

        // Act
        await sut.TransferCommand.ExecuteAsync(null);

        // Assert
        sut.TransferStatus.Should().Be("Transfer miktari mevcut stoktan buyuk olamaz.");
    }
}
