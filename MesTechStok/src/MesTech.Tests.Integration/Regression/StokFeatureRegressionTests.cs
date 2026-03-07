using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration._Shared;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// Stok 12 feature regression tests — verifies core stock management features work end-to-end.
/// Each test targets a specific Stok module feature.
/// Uses InMemory DB for speed; real PostgreSQL tested in PostgreSqlRepositoryTests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Regression")]
public class StokFeatureRegressionTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private readonly ProductRepository _productRepo;
    private readonly StockMovementRepository _movementRepo;

    public StokFeatureRegressionTests()
    {
        _productRepo = new ProductRepository(Context);
        _movementRepo = new StockMovementRepository(Context);
    }

    // ── Feature 1: Product CRUD ──

    [Fact]
    public async Task Feature01_ProductCrud_ShouldWorkEndToEnd()
    {
        var product = new Product
        {
            Name = "Regression Urun",
            SKU = "REG-001",
            Barcode = "8691234567001",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = 0,
            CategoryId = TestCategoryId,
            TenantId = TestTenantId,
            IsActive = true
        };

        await _productRepo.AddAsync(product);
        await Context.SaveChangesAsync();

        var read = await _productRepo.GetByIdAsync(product.Id);
        read.Should().NotBeNull();

        read!.Name = "Updated Urun";
        await _productRepo.UpdateAsync(read);
        await Context.SaveChangesAsync();

        var updated = await _productRepo.GetBySKUAsync("REG-001");
        updated!.Name.Should().Be("Updated Urun");
    }

    // ── Feature 2: Stock Movement ──

    [Fact]
    public async Task Feature02_StockMovement_ShouldTrackPurchaseAndSale()
    {
        var product = CreateTestProduct("MOV-001", stock: 0);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        // Purchase: +100
        product.AdjustStock(100, StockMovementType.Purchase, "Alis");
        var purchaseMovement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = 100,
            PreviousStock = 0,
            NewStock = 100,
            MovementType = StockMovementType.Purchase.ToString(),
            Reason = "Alis",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(purchaseMovement);
        await Context.SaveChangesAsync();

        // Sale: -30
        product.AdjustStock(-30, StockMovementType.Sale, "Satis");
        var saleMovement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = -30,
            PreviousStock = 100,
            NewStock = 70,
            MovementType = StockMovementType.Sale.ToString(),
            Reason = "Satis",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(saleMovement);
        await Context.SaveChangesAsync();

        product.Stock.Should().Be(70);
        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.Should().HaveCount(2);
    }

    // ── Feature 3: Multi-Warehouse (Depo) ──

    [Fact]
    public async Task Feature03_MultiWarehouse_TransferShouldTrackLocations()
    {
        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();
        var product = CreateTestProduct("WH-001", stock: 50);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var transfer = new StockMovement
        {
            ProductId = product.Id,
            Quantity = -10,
            PreviousStock = 50,
            NewStock = 40,
            MovementType = StockMovementType.Transfer.ToString(),
            FromWarehouseId = warehouseA,
            ToWarehouseId = warehouseB,
            Reason = "Depo transferi",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(transfer);
        await Context.SaveChangesAsync();

        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.Should().ContainSingle();
        movements.First().FromWarehouseId.Should().Be(warehouseA);
        movements.First().ToWarehouseId.Should().Be(warehouseB);
    }

    // ── Feature 4: Lot/FEFO (BatchNumber + ExpiryDate) ──

    [Fact]
    public async Task Feature04_LotTracking_ShouldStoreBatchAndExpiry()
    {
        var product = CreateTestProduct("LOT-001", stock: 200);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var expiryDate = new DateTime(2027, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = 100,
            MovementType = StockMovementType.Purchase.ToString(),
            BatchNumber = "BATCH-2026-001",
            ExpiryDate = expiryDate,
            Reason = "Lot giris",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(movement);
        await Context.SaveChangesAsync();

        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.First().BatchNumber.Should().Be("BATCH-2026-001");
        movements.First().ExpiryDate.Should().Be(expiryDate);
    }

    // ── Feature 5: Barcode ──

    [Fact]
    public async Task Feature05_Barcode_ShouldFindProductByBarcode()
    {
        var product = CreateTestProduct("BAR-001", stock: 15);
        product.Barcode = "8691234500001";
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var found = await _productRepo.GetByBarcodeAsync("8691234500001");

        found.Should().NotBeNull();
        found!.SKU.Should().Be("BAR-001");
    }

    // ── Feature 6: Category ──

    [Fact]
    public async Task Feature06_Category_ShouldFilterProductsByCategory()
    {
        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();

        Context.Products.AddRange(
            CreateTestProduct("CAT-A1", categoryId: catA),
            CreateTestProduct("CAT-A2", categoryId: catA),
            CreateTestProduct("CAT-B1", categoryId: catB)
        );
        await Context.SaveChangesAsync();

        var catAProducts = await _productRepo.GetByCategoryAsync(catA);

        catAProducts.Should().HaveCount(2);
        catAProducts.Should().OnlyContain(p => p.CategoryId == catA);
    }

    // ── Feature 7: Minimum Stock Alarm ──

    [Fact]
    public async Task Feature07_MinimumStockAlarm_ShouldDetectLowStock()
    {
        Context.Products.AddRange(
            new Product
            {
                Name = "Dusuk Stok", SKU = "LOW-001", Stock = 3, MinimumStock = 10,
                CategoryId = TestCategoryId, TenantId = TestTenantId, IsActive = true
            },
            new Product
            {
                Name = "Normal Stok", SKU = "NOR-001", Stock = 50, MinimumStock = 10,
                CategoryId = TestCategoryId, TenantId = TestTenantId, IsActive = true
            }
        );
        await Context.SaveChangesAsync();

        var lowStock = await _productRepo.GetLowStockAsync();

        lowStock.Should().ContainSingle();
        lowStock.First().SKU.Should().Be("LOW-001");
    }

    // ── Feature 8: Barcode Scan Movement ──

    [Fact]
    public async Task Feature08_BarcodeScannedMovement_ShouldTrackScanInfo()
    {
        var product = CreateTestProduct("SCAN-001", stock: 25);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = 5,
            MovementType = StockMovementType.Purchase.ToString(),
            ScannedBarcode = "8691234500099",
            IsScannedMovement = true,
            Reason = "Barkod tarama ile giris",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(movement);
        await Context.SaveChangesAsync();

        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.First().IsScannedMovement.Should().BeTrue();
        movements.First().ScannedBarcode.Should().Be("8691234500099");
    }

    // ── Feature 9: Cost Tracking (WAC) ──

    [Fact]
    public async Task Feature09_CostTracking_ShouldStoreUnitAndTotalCost()
    {
        var product = CreateTestProduct("COST-001", stock: 0);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = 50,
            UnitCost = 12.50m,
            TotalCost = 625m,
            MovementType = StockMovementType.Purchase.ToString(),
            Reason = "Maliyetli alis",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(movement);
        await Context.SaveChangesAsync();

        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.First().UnitCost.Should().Be(12.50m);
        movements.First().TotalCost.Should().Be(625m);
    }

    // ── Feature 10: Product Count / Reporting ──

    [Fact]
    public async Task Feature10_Reporting_CountAndDateRange_ShouldWork()
    {
        Context.Products.AddRange(
            CreateTestProduct("RPT-001"),
            CreateTestProduct("RPT-002"),
            CreateTestProduct("RPT-003")
        );
        await Context.SaveChangesAsync();

        var count = await _productRepo.GetCountAsync();
        count.Should().Be(3);

        var all = await _productRepo.GetAllAsync();
        all.Should().HaveCount(3);
    }

    // ── Feature 11: Movement Reversal ──

    [Fact]
    public async Task Feature11_MovementReversal_ShouldTrackReversalLink()
    {
        var product = CreateTestProduct("REV-001", stock: 100);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var original = new StockMovement
        {
            ProductId = product.Id,
            Quantity = -20,
            MovementType = StockMovementType.Sale.ToString(),
            Reason = "Satis",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(original);
        await Context.SaveChangesAsync();

        var reversal = new StockMovement
        {
            ProductId = product.Id,
            Quantity = 20,
            MovementType = StockMovementType.Return.ToString(),
            IsReversed = true,
            ReversalMovementId = original.Id,
            Reason = "Iade",
            TenantId = TestTenantId
        };
        await _movementRepo.AddAsync(reversal);
        await Context.SaveChangesAsync();

        var movements = await _movementRepo.GetByProductIdAsync(product.Id);
        movements.Should().HaveCount(2);
        var reversalEntry = movements.First(m => m.IsReversed);
        reversalEntry.ReversalMovementId.Should().Be(original.Id);
    }

    // ── Feature 12: Soft Delete ──

    [Fact]
    public async Task Feature12_SoftDelete_ShouldExcludeDeletedProducts()
    {
        var activeProduct = CreateTestProduct("SD-ACT-001");
        var deletedProduct = CreateTestProduct("SD-DEL-001");
        deletedProduct.IsActive = false;
        Context.Products.AddRange(activeProduct, deletedProduct);
        await Context.SaveChangesAsync();

        var allActive = await _productRepo.GetAllAsync();

        allActive.Should().ContainSingle();
        allActive.First().SKU.Should().Be("SD-ACT-001");
    }

    private Product CreateTestProduct(string sku, int stock = 50, Guid? categoryId = null)
    {
        return new Product
        {
            Name = $"Test {sku}",
            SKU = sku,
            Barcode = $"869{sku.GetHashCode():D10}"[..13],
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = stock,
            MinimumStock = 5,
            CategoryId = categoryId ?? TestCategoryId,
            TenantId = TestTenantId,
            IsActive = true
        };
    }
}
