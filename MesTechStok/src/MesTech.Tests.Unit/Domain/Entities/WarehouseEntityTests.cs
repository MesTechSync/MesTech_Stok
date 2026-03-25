using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Warehouse domain entity tests: Warehouse, WarehouseZone, WarehouseRack,
/// WarehouseShelf, WarehouseBin, StockMovement, StockAlert,
/// ProductWarehouseStock, InventoryLot, BarcodeScanLog.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "WarehouseEntities")]
[Trait("Phase", "Dalga15")]
public class WarehouseEntityTests
{
    // ═══════════════════════════════════════════
    // Warehouse
    // ═══════════════════════════════════════════

    [Fact]
    public void Warehouse_Creation_SetsDefaults()
    {
        var wh = new Warehouse();

        wh.Id.Should().NotBe(Guid.Empty);
        wh.Type.Should().Be("MAIN");
        wh.IsActive.Should().BeTrue();
        wh.IsDefault.Should().BeFalse();
        wh.Name.Should().BeEmpty();
    }

    [Fact]
    public void Warehouse_SetAsDefault_SetsFlag()
    {
        var wh = new Warehouse();
        wh.SetAsDefault();
        wh.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Warehouse_UnsetDefault_ClearsFlag()
    {
        var wh = new Warehouse();
        wh.SetAsDefault();
        wh.UnsetDefault();
        wh.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Warehouse_UpdateCosts_SetsBothValues()
    {
        var wh = new Warehouse();
        wh.UpdateCosts(15000m, 250m);

        wh.MonthlyCost.Should().Be(15000m);
        wh.CostPerSquareMeter.Should().Be(250m);
    }

    [Fact]
    public void Warehouse_DisplayName_WithCode_IncludesBrackets()
    {
        var wh = new Warehouse { Name = "Ana Depo", Code = "WH-01" };
        wh.DisplayName.Should().Be("[WH-01] Ana Depo");
    }

    [Fact]
    public void Warehouse_DisplayName_WithoutCode_ReturnsNameOnly()
    {
        var wh = new Warehouse { Name = "Ana Depo" };
        wh.DisplayName.Should().Be("Ana Depo");
    }

    [Fact]
    public void Warehouse_ToString_ReturnsDisplayName()
    {
        var wh = new Warehouse { Name = "Depo", Code = "D1" };
        wh.ToString().Should().Be("[D1] Depo");
    }

    // ═══════════════════════════════════════════
    // WarehouseZone
    // ═══════════════════════════════════════════

    [Fact]
    public void WarehouseZone_Creation_SetsDefaults()
    {
        var zone = new WarehouseZone();
        zone.Id.Should().NotBe(Guid.Empty);
        zone.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WarehouseZone_ActivateDeactivate_TogglesState()
    {
        var zone = new WarehouseZone();
        zone.Deactivate();
        zone.IsActive.Should().BeFalse();
        zone.Activate();
        zone.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // WarehouseRack
    // ═══════════════════════════════════════════

    [Fact]
    public void WarehouseRack_Creation_SetsDefaults()
    {
        var rack = new WarehouseRack();
        rack.Id.Should().NotBe(Guid.Empty);
        rack.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WarehouseRack_ActivateDeactivate_TogglesState()
    {
        var rack = new WarehouseRack();
        rack.Deactivate();
        rack.IsActive.Should().BeFalse();
        rack.Activate();
        rack.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // WarehouseShelf
    // ═══════════════════════════════════════════

    [Fact]
    public void WarehouseShelf_Creation_SetsDefaults()
    {
        var shelf = new WarehouseShelf();
        shelf.Id.Should().NotBe(Guid.Empty);
        shelf.IsActive.Should().BeTrue();
        shelf.IsAccessible.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // WarehouseBin
    // ═══════════════════════════════════════════

    [Fact]
    public void WarehouseBin_Creation_SetsDefaults()
    {
        var bin = new WarehouseBin();
        bin.Id.Should().NotBe(Guid.Empty);
        bin.IsActive.Should().BeTrue();
        bin.IsReserved.Should().BeFalse();
        bin.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_Reserve_SetsFlag()
    {
        var bin = new WarehouseBin();
        bin.Reserve();
        bin.IsReserved.Should().BeTrue();
    }

    [Fact]
    public void WarehouseBin_Reserve_WhenLocked_Throws()
    {
        var bin = new WarehouseBin();
        bin.Lock();

        var act = () => bin.Reserve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WarehouseBin_ReleaseReservation_ClearsFlag()
    {
        var bin = new WarehouseBin();
        bin.Reserve();
        bin.ReleaseReservation();
        bin.IsReserved.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_Lock_SetsLockedAndClearsReservation()
    {
        var bin = new WarehouseBin();
        bin.Reserve();
        bin.Lock();

        bin.IsLocked.Should().BeTrue();
        bin.IsReserved.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_Unlock_ClearsFlag()
    {
        var bin = new WarehouseBin();
        bin.Lock();
        bin.Unlock();
        bin.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_IsAvailable_TrueWhenActiveAndFree()
    {
        var bin = new WarehouseBin();
        bin.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void WarehouseBin_IsAvailable_FalseWhenReserved()
    {
        var bin = new WarehouseBin();
        bin.Reserve();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_IsAvailable_FalseWhenLocked()
    {
        var bin = new WarehouseBin();
        bin.Lock();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_IsAvailable_FalseWhenDeactivated()
    {
        var bin = new WarehouseBin();
        bin.Deactivate();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void WarehouseBin_ActivateDeactivate_TogglesState()
    {
        var bin = new WarehouseBin();
        bin.Deactivate();
        bin.IsActive.Should().BeFalse();
        bin.Activate();
        bin.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // StockMovement
    // ═══════════════════════════════════════════

    [Fact]
    public void StockMovement_Creation_SetsDefaults()
    {
        var sm = new StockMovement();
        sm.Id.Should().NotBe(Guid.Empty);
        sm.IsApproved.Should().BeFalse();
        sm.IsReversed.Should().BeFalse();
        sm.MovementType.Should().BeEmpty();
    }

    [Fact]
    public void StockMovement_SetMovementType_SetsStringValue()
    {
        var sm = new StockMovement();
        sm.SetMovementType(StockMovementType.StockIn);
        sm.MovementType.Should().Be("StockIn");
    }

    [Fact]
    public void StockMovement_SetStockLevels_SetsAllLevels()
    {
        var sm = new StockMovement();
        sm.SetStockLevels(50, 70);

        sm.PreviousStock.Should().Be(50);
        sm.NewStock.Should().Be(70);
        sm.NewStockLevel.Should().Be(70);
    }

    [Fact]
    public void StockMovement_Approve_SetsApproverAndFlag()
    {
        var sm = new StockMovement();
        sm.Approve("admin@test.com");

        sm.IsApproved.Should().BeTrue();
        sm.ApprovedBy.Should().Be("admin@test.com");
        sm.ApprovedDate.Should().NotBeNull();
    }

    [Fact]
    public void StockMovement_Approve_EmptyApprover_Throws()
    {
        var sm = new StockMovement();
        var act = () => sm.Approve("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StockMovement_MarkAsReversed_SetsFlags()
    {
        var sm = new StockMovement();
        var reversalId = Guid.NewGuid();
        sm.MarkAsReversed(reversalId);

        sm.IsReversed.Should().BeTrue();
        sm.ReversalMovementId.Should().Be(reversalId);
    }

    [Fact]
    public void StockMovement_IsPositiveMovement_TrueForPositiveQty()
    {
        var sm = new StockMovement { Quantity = 10 };
        sm.IsPositiveMovement.Should().BeTrue();
        sm.IsNegativeMovement.Should().BeFalse();
    }

    [Fact]
    public void StockMovement_IsNegativeMovement_TrueForNegativeQty()
    {
        var sm = new StockMovement { Quantity = -5 };
        sm.IsNegativeMovement.Should().BeTrue();
        sm.IsPositiveMovement.Should().BeFalse();
    }

    [Fact]
    public void StockMovement_ZeroQuantity_BothFalse()
    {
        var sm = new StockMovement { Quantity = 0 };
        sm.IsPositiveMovement.Should().BeFalse();
        sm.IsNegativeMovement.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // StockAlert
    // ═══════════════════════════════════════════

    [Fact]
    public void StockAlert_Creation_SetsDefaults()
    {
        var alert = new StockAlert();
        alert.Id.Should().NotBe(Guid.Empty);
        alert.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void StockAlert_Resolve_SetsResolvedState()
    {
        var alert = new StockAlert();
        alert.Resolve("admin");

        alert.IsResolved.Should().BeTrue();
        alert.ResolvedBy.Should().Be("admin");
        alert.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void StockAlert_Resolve_EmptyResolver_Throws()
    {
        var alert = new StockAlert();
        var act = () => alert.Resolve("");
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // ProductWarehouseStock
    // ═══════════════════════════════════════════

    [Fact]
    public void ProductWarehouseStock_Create_SetsProperties()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var pws = ProductWarehouseStock.Create(productId, warehouseId, "AmazonFBA");

        pws.ProductId.Should().Be(productId);
        pws.WarehouseId.Should().Be(warehouseId);
        pws.FulfillmentCenter.Should().Be("AmazonFBA");
        pws.AvailableQuantity.Should().Be(0);
        pws.ReservedQuantity.Should().Be(0);
        pws.InboundQuantity.Should().Be(0);
    }

    [Fact]
    public void ProductWarehouseStock_Create_EmptyProductId_Throws()
    {
        var act = () => ProductWarehouseStock.Create(Guid.Empty, Guid.NewGuid(), "OwnWarehouse");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_Create_EmptyWarehouseId_Throws()
    {
        var act = () => ProductWarehouseStock.Create(Guid.NewGuid(), Guid.Empty, "OwnWarehouse");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_Create_EmptyFulfillment_Throws()
    {
        var act = () => ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_UpdateStock_ValidValues_SetsCorrectly()
    {
        var pws = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        pws.UpdateStock(100, 20, 50);

        pws.AvailableQuantity.Should().Be(100);
        pws.ReservedQuantity.Should().Be(20);
        pws.InboundQuantity.Should().Be(50);
    }

    [Fact]
    public void ProductWarehouseStock_UpdateStock_NegativeAvailable_Throws()
    {
        var pws = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        var act = () => pws.UpdateStock(-1, 0, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_UpdateStock_NegativeReserved_Throws()
    {
        var pws = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        var act = () => pws.UpdateStock(0, -1, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_UpdateStock_NegativeInbound_Throws()
    {
        var pws = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        var act = () => pws.UpdateStock(0, 0, -1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductWarehouseStock_TotalQuantity_ComputedCorrectly()
    {
        var pws = ProductWarehouseStock.Create(Guid.NewGuid(), Guid.NewGuid(), "OwnWarehouse");
        pws.UpdateStock(80, 20, 10);

        pws.TotalQuantity.Should().Be(100);
    }

    // ═══════════════════════════════════════════
    // InventoryLot
    // ═══════════════════════════════════════════

    [Fact]
    public void InventoryLot_Creation_SetsDefaults()
    {
        var lot = new InventoryLot();
        lot.Id.Should().NotBe(Guid.Empty);
        lot.Status.Should().Be(LotStatus.Open);
        lot.LotNumber.Should().BeEmpty();
    }

    [Fact]
    public void InventoryLot_Consume_ReducesRemainingQty()
    {
        var lot = new InventoryLot { ReceivedQty = 100, RemainingQty = 100 };
        lot.Consume(30);

        lot.RemainingQty.Should().Be(70);
        lot.Status.Should().Be(LotStatus.Open);
    }

    [Fact]
    public void InventoryLot_Consume_AllRemaining_ClosesLot()
    {
        var lot = new InventoryLot { ReceivedQty = 50, RemainingQty = 50 };
        lot.Consume(50);

        lot.RemainingQty.Should().Be(0);
        lot.Status.Should().Be(LotStatus.Closed);
        lot.ClosedDate.Should().NotBeNull();
    }

    [Fact]
    public void InventoryLot_Consume_MoreThanRemaining_Throws()
    {
        var lot = new InventoryLot { ReceivedQty = 10, RemainingQty = 10 };
        var act = () => lot.Consume(20);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void InventoryLot_IsExpired_TrueWhenPastExpiry()
    {
        var lot = new InventoryLot { ExpiryDate = DateTime.UtcNow.AddDays(-1) };
        lot.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void InventoryLot_IsExpired_FalseWhenNotExpired()
    {
        var lot = new InventoryLot { ExpiryDate = DateTime.UtcNow.AddDays(30) };
        lot.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void InventoryLot_IsExpired_FalseWhenNoExpiryDate()
    {
        var lot = new InventoryLot();
        lot.IsExpired.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // BarcodeScanLog
    // ═══════════════════════════════════════════

    [Fact]
    public void BarcodeScanLog_Creation_SetsDefaults()
    {
        var log = new BarcodeScanLog();
        log.Id.Should().NotBe(Guid.Empty);
        log.Barcode.Should().BeEmpty();
        log.Format.Should().BeEmpty();
        log.Source.Should().BeEmpty();
    }
}
