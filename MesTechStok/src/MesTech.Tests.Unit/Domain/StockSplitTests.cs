using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// StockSplit domain testleri — ProductWarehouseStock uzerinden coklu depo/fulfillment center
/// stok dagilimi ve guncelleme senaryolari.
/// </summary>
[Trait("Category", "Unit")]
public class StockSplitTests
{
    // ── 1. Create_MultipleWarehouses_SameProduct ─────────────────────────────

    [Fact]
    public void Create_MultipleWarehouses_SameProduct_DifferentCenters()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseOwn = Guid.NewGuid();
        var warehouseFba = Guid.NewGuid();
        var warehouseHl = Guid.NewGuid();

        // Act — same productId, three different fulfillment centers
        var ownStock = ProductWarehouseStock.Create(productId, warehouseOwn, "OwnWarehouse");
        var fbaStock = ProductWarehouseStock.Create(productId, warehouseFba, "AmazonFBA");
        var hlStock  = ProductWarehouseStock.Create(productId, warehouseHl, "Hepsilojistik");

        // Assert — all share same product but each has a unique center + id
        ownStock.ProductId.Should().Be(productId);
        fbaStock.ProductId.Should().Be(productId);
        hlStock.ProductId.Should().Be(productId);

        ownStock.FulfillmentCenter.Should().Be("OwnWarehouse");
        fbaStock.FulfillmentCenter.Should().Be("AmazonFBA");
        hlStock.FulfillmentCenter.Should().Be("Hepsilojistik");

        new[] { ownStock.Id, fbaStock.Id, hlStock.Id }.Should().OnlyHaveUniqueItems();
    }

    // ── 2. UpdateStock_SetsLastSyncedAt ──────────────────────────────────────

    [Fact]
    public void UpdateStock_SetsLastSyncedAt()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        var before = DateTime.UtcNow;

        // Act
        stock.UpdateStock(available: 50, reserved: 10, inbound: 5);

        // Assert
        stock.LastSyncedAt.Should().BeOnOrAfter(before);
        stock.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ── 3. TotalQuantity_SumOfAvailableAndReserved ───────────────────────────

    [Fact]
    public void TotalQuantity_SumOfAvailableAndReserved()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act
        stock.UpdateStock(available: 80, reserved: 30, inbound: 10);

        // Assert — TotalQuantity = Available + Reserved (inbound is NOT included)
        stock.TotalQuantity.Should().Be(110);
        stock.AvailableQuantity.Should().Be(80);
        stock.ReservedQuantity.Should().Be(30);
        stock.InboundQuantity.Should().Be(10);
    }

    // ── 4. GetTotalAvailable_AcrossWarehouses ────────────────────────────────

    [Fact]
    public void GetTotalAvailable_AcrossWarehouses_SumsAllAvailableQuantities()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var ownStock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), "OwnWarehouse");
        ownStock.UpdateStock(available: 100, reserved: 10, inbound: 20);

        var fbaStock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), "AmazonFBA");
        fbaStock.UpdateStock(available: 50, reserved: 5, inbound: 0);

        var hlStock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), "Hepsilojistik");
        hlStock.UpdateStock(available: 30, reserved: 0, inbound: 10);

        var allStocks = new[] { ownStock, fbaStock, hlStock };

        // Act — simulate "GetTotalAvailable" as a LINQ sum across warehouse records
        var totalAvailable = allStocks.Sum(s => s.AvailableQuantity);

        // Assert
        totalAvailable.Should().Be(180); // 100 + 50 + 30
    }

    // ── 5. UpdateFulfillmentStock_SpecificCenter_OnlyThatCenterUpdated ────────

    [Fact]
    public void UpdateFulfillmentStock_SpecificCenter_OnlyThatCenterUpdated()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var ownStock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), "OwnWarehouse");
        ownStock.UpdateStock(available: 100, reserved: 10, inbound: 0);

        var fbaStock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), "AmazonFBA");
        fbaStock.UpdateStock(available: 40, reserved: 5, inbound: 0);

        // Act — update only FBA center
        fbaStock.UpdateStock(available: 25, reserved: 3, inbound: 10);

        // Assert — FBA updated, OwnWarehouse unchanged
        fbaStock.AvailableQuantity.Should().Be(25);
        fbaStock.ReservedQuantity.Should().Be(3);
        fbaStock.InboundQuantity.Should().Be(10);

        ownStock.AvailableQuantity.Should().Be(100);
        ownStock.ReservedQuantity.Should().Be(10);
        ownStock.InboundQuantity.Should().Be(0);
    }

    // ── 6. Create_OwnWarehouse_DefaultCenter ─────────────────────────────────

    [Fact]
    public void Create_OwnWarehouse_DefaultCenter()
    {
        // Arrange & Act
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Assert — default fulfillment center is "OwnWarehouse"
        stock.FulfillmentCenter.Should().Be("OwnWarehouse");
        stock.AvailableQuantity.Should().Be(0);
        stock.ReservedQuantity.Should().Be(0);
        stock.InboundQuantity.Should().Be(0);
    }

    // ── 7. Create_AmazonFBA_CorrectCenter ────────────────────────────────────

    [Fact]
    public void Create_AmazonFBA_CorrectCenter()
    {
        // Arrange & Act
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "AmazonFBA");

        // Assert
        stock.FulfillmentCenter.Should().Be("AmazonFBA");
        stock.ProductId.Should().NotBe(Guid.Empty);
        stock.WarehouseId.Should().NotBe(Guid.Empty);
    }

    // ── 8. UpdateStock_ZeroQuantity_Allowed ──────────────────────────────────

    [Fact]
    public void UpdateStock_ZeroQuantity_Allowed()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "Hepsilojistik");
        stock.UpdateStock(available: 50, reserved: 20, inbound: 10);

        // Act — reset all quantities to zero (e.g., after stock-out)
        stock.UpdateStock(available: 0, reserved: 0, inbound: 0);

        // Assert — zero is valid, no exception thrown
        stock.AvailableQuantity.Should().Be(0);
        stock.ReservedQuantity.Should().Be(0);
        stock.InboundQuantity.Should().Be(0);
        stock.TotalQuantity.Should().Be(0);
    }

    // ── 9. UpdateStock_NegativeQuantity_Throws ───────────────────────────────

    [Fact]
    public void UpdateStock_NegativeQuantity_Throws()
    {
        // Arrange
        var stock = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");

        // Act & Assert — each negative param throws ArgumentException
        var actAvailable = () => stock.UpdateStock(available: -1, reserved: 0, inbound: 0);
        actAvailable.Should().Throw<ArgumentException>().WithParameterName("available");

        var actReserved = () => stock.UpdateStock(available: 0, reserved: -5, inbound: 0);
        actReserved.Should().Throw<ArgumentException>().WithParameterName("reserved");

        var actInbound = () => stock.UpdateStock(available: 0, reserved: 0, inbound: -3);
        actInbound.Should().Throw<ArgumentException>().WithParameterName("inbound");
    }

    // ── 10. MultipleProducts_IndependentStocks ───────────────────────────────

    [Fact]
    public void MultipleProducts_IndependentStocks()
    {
        // Arrange — two different products in the same warehouse/center
        var warehouseId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        var stockA = ProductWarehouseStock.Create(productA, warehouseId, "OwnWarehouse");
        var stockB = ProductWarehouseStock.Create(productB, warehouseId, "OwnWarehouse");

        // Act
        stockA.UpdateStock(available: 200, reserved: 50, inbound: 0);
        stockB.UpdateStock(available: 10, reserved: 2, inbound: 100);

        // Assert — products are completely independent, no cross-contamination
        stockA.ProductId.Should().Be(productA);
        stockA.AvailableQuantity.Should().Be(200);
        stockA.TotalQuantity.Should().Be(250);

        stockB.ProductId.Should().Be(productB);
        stockB.AvailableQuantity.Should().Be(10);
        stockB.TotalQuantity.Should().Be(12);

        stockA.Id.Should().NotBe(stockB.Id);
    }
}
