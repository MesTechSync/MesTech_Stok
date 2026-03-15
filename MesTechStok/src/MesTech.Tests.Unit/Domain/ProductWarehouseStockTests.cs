using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class ProductWarehouseStockTests
{
    [Fact]
    public void Create_ValidParams_Success()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var center = "AmazonFBA";

        // Act
        var stock = ProductWarehouseStock.Create(productId, warehouseId, center);

        // Assert
        stock.Should().NotBeNull();
        stock.ProductId.Should().Be(productId);
        stock.WarehouseId.Should().Be(warehouseId);
        stock.FulfillmentCenter.Should().Be(center);
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        // Arrange & Act
        var act = () => ProductWarehouseStock.Create(Guid.Empty, Guid.NewGuid(), "OwnWarehouse");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("productId");
    }

    [Fact]
    public void UpdateStock_ValidValues_UpdatesAll()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act
        stock.UpdateStock(available: 100, reserved: 20, inbound: 50);

        // Assert
        stock.AvailableQuantity.Should().Be(100);
        stock.ReservedQuantity.Should().Be(20);
        stock.InboundQuantity.Should().Be(50);
    }

    [Fact]
    public void UpdateStock_NegativeAvailable_Throws()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act
        var act = () => stock.UpdateStock(available: -1, reserved: 0, inbound: 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("available");
    }

    [Fact]
    public void TotalQuantity_Calculated_Correctly()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "Hepsilojistik");
        stock.UpdateStock(available: 80, reserved: 30, inbound: 10);

        // Act & Assert
        stock.TotalQuantity.Should().Be(110); // 80 + 30
    }

    [Fact]
    public void UpdateStock_SetsLastSyncedAt()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        var beforeUpdate = DateTime.UtcNow;

        // Act
        stock.UpdateStock(available: 50, reserved: 10, inbound: 5);

        // Assert
        stock.LastSyncedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void Create_DefaultValues_Zero()
    {
        // Arrange & Act
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Assert
        stock.AvailableQuantity.Should().Be(0);
        stock.ReservedQuantity.Should().Be(0);
        stock.InboundQuantity.Should().Be(0);
        stock.TotalQuantity.Should().Be(0);
    }

    [Fact]
    public void UpdateStock_ZeroValues_Allowed()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        stock.UpdateStock(available: 100, reserved: 20, inbound: 50);

        // Act
        stock.UpdateStock(available: 0, reserved: 0, inbound: 0);

        // Assert
        stock.AvailableQuantity.Should().Be(0);
        stock.ReservedQuantity.Should().Be(0);
        stock.InboundQuantity.Should().Be(0);
    }

    [Fact]
    public void FulfillmentCenter_AmazonFBA_Correct()
    {
        // Arrange & Act
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "AmazonFBA");

        // Assert
        stock.FulfillmentCenter.Should().Be("AmazonFBA");
    }

    [Fact]
    public void Create_DifferentCenters_SameProduct_Allowed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouse1 = Guid.NewGuid();
        var warehouse2 = Guid.NewGuid();

        // Act
        var stockOwn = ProductWarehouseStock.Create(productId, warehouse1, "OwnWarehouse");
        var stockFba = ProductWarehouseStock.Create(productId, warehouse2, "AmazonFBA");

        // Assert
        stockOwn.ProductId.Should().Be(productId);
        stockFba.ProductId.Should().Be(productId);
        stockOwn.FulfillmentCenter.Should().NotBe(stockFba.FulfillmentCenter);
        stockOwn.Id.Should().NotBe(stockFba.Id);
    }

    [Fact]
    public void Create_EmptyWarehouseId_Throws()
    {
        // Arrange & Act
        var act = () => ProductWarehouseStock.Create(Guid.NewGuid(), Guid.Empty, "OwnWarehouse");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("warehouseId");
    }

    [Fact]
    public void UpdateStock_NegativeReserved_Throws()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act
        var act = () => stock.UpdateStock(available: 10, reserved: -5, inbound: 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("reserved");
    }

    [Fact]
    public void UpdateStock_NegativeInbound_Throws()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act
        var act = () => stock.UpdateStock(available: 10, reserved: 0, inbound: -3);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("inbound");
    }

    [Fact]
    public void Create_NullFulfillmentCenter_Throws()
    {
        // Arrange & Act
        var act = () => ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
