using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using AcctType = MesTech.Domain.Accounting.Enums.AccountType;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless UI test seed data — her ana entity için 3-5 gerçekçi kayıt.
/// TenantId tümünde aynı. FK ilişkileri doğru.
/// </summary>
public static class TestSeedDataFactory
{
    public static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Product>().AnyAsync()) return; // idempotent

        // ── Tenant (FK zorunlu — tüm entity'lerin TenantId'si buna referans verir) ──
        var tenant = new Tenant { Name = "MesTech Test Tenant", IsActive = true };
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, TestTenantId);
        db.Set<Tenant>().Add(tenant);
        await db.SaveChangesAsync(); // Tenant önce kaydedilmeli — FK constraint

        // ── Categories (hiyerarşik) ──
        var catElektronik = Category.Create(TestTenantId, "Elektronik", "ELK");
        var catGiyim = Category.Create(TestTenantId, "Giyim", "GYM");
        var catEv = Category.Create(TestTenantId, "Ev & Yaşam", "EVY");
        var catTelefon = Category.Create(TestTenantId, "Telefon", "TEL");
        catTelefon.ParentCategoryId = catElektronik.Id;
        var catAksesuar = Category.Create(TestTenantId, "Aksesuar", "AKS");
        catAksesuar.ParentCategoryId = catElektronik.Id;
        db.Set<Category>().AddRange(catElektronik, catGiyim, catEv, catTelefon, catAksesuar);

        // ── Suppliers ──
        var sup1 = Supplier.Create(TestTenantId, "ABC Elektronik AŞ", "SUP-001", "info@abcelektronik.com", "02121234567");
        var sup2 = Supplier.Create(TestTenantId, "Mega Tekstil Ltd", "SUP-002", "siparis@megatekstil.com", "02161234567");
        db.Set<Supplier>().AddRange(sup1, sup2);

        // ── Customers ──
        var cust1 = Customer.Create(TestTenantId, "Ahmet Yılmaz", "MUS-001", "ahmet@test.com", "05301234567");
        cust1.City = "İstanbul";
        cust1.BillingAddress = "Kadıköy, İstanbul";
        var cust2 = Customer.Create(TestTenantId, "Fatma Demir", "MUS-002", "fatma@test.com", "05351234567");
        cust2.City = "Ankara";
        var cust3 = Customer.Create(TestTenantId, "Mehmet Kaya", "MUS-003", "mehmet@test.com", "05401234567");
        cust3.City = "İzmir";
        db.Set<Customer>().AddRange(cust1, cust2, cust3);

        // ── Products ──
        var prod1 = CreateProduct("iPhone 15 Pro Max", "SKU-IPH15PM", catTelefon.Id, 64999.99m, 120);
        var prod2 = CreateProduct("Samsung Galaxy S24", "SKU-SGS24", catTelefon.Id, 42999.99m, 85);
        var prod3 = CreateProduct("Nike Air Max 90", "SKU-NAM90", catGiyim.Id, 3499.99m, 200);
        var prod4 = CreateProduct("USB-C Kablo 2m", "SKU-USBC2M", catAksesuar.Id, 149.99m, 500);
        var prod5 = CreateProduct("Kahve Makinesi DeLonghi", "SKU-DLKM01", catEv.Id, 12999.99m, 30);
        db.Set<Product>().AddRange(prod1, prod2, prod3, prod4, prod5);

        // ── Orders ──
        var order1 = Order.CreateManual(TestTenantId, cust1.Id, "Ahmet Yılmaz", "ahmet@test.com", "SALE");
        order1.Place();
        var order2 = Order.CreateManual(TestTenantId, cust2.Id, "Fatma Demir", "fatma@test.com", "SALE");
        var order3 = Order.CreateManual(TestTenantId, cust3.Id, "Mehmet Kaya", "mehmet@test.com", "SALE");
        order3.Place();
        order3.MarkAsShipped("TR1234567890", CargoProvider.YurticiKargo);
        db.Set<Order>().AddRange(order1, order2, order3);

        // ── OrderItems ──
        db.Set<OrderItem>().AddRange(
            CreateOrderItem(order1.Id, prod1.Id, "iPhone 15 Pro Max", "SKU-IPH15PM", 1, 64999.99m),
            CreateOrderItem(order1.Id, prod4.Id, "USB-C Kablo 2m", "SKU-USBC2M", 2, 149.99m),
            CreateOrderItem(order2.Id, prod3.Id, "Nike Air Max 90", "SKU-NAM90", 1, 3499.99m),
            CreateOrderItem(order3.Id, prod2.Id, "Samsung Galaxy S24", "SKU-SGS24", 1, 42999.99m),
            CreateOrderItem(order3.Id, prod5.Id, "Kahve Makinesi", "SKU-DLKM01", 1, 12999.99m));

        // ── StockMovements ──
        db.Set<StockMovement>().AddRange(
            CreateStockMovement(prod1.Id, TestTenantId, StockMovementType.Purchase, 120, "İlk stok girişi"),
            CreateStockMovement(prod2.Id, TestTenantId, StockMovementType.Purchase, 85, "İlk stok girişi"),
            CreateStockMovement(prod3.Id, TestTenantId, StockMovementType.Purchase, 200, "İlk stok girişi"));

        // ── Notifications ──
        db.Set<NotificationLog>().AddRange(
            NotificationLog.Create(TestTenantId, NotificationChannel.Push, "dashboard", "OrderReceived", "Yeni sipariş #1 alındı"),
            NotificationLog.Create(TestTenantId, NotificationChannel.Push, "dashboard", "LowStock", "USB-C Kablo stok düşük"),
            NotificationLog.Create(TestTenantId, NotificationChannel.Email, "ahmet@test.com", "ShipmentTracking", "Kargonuz yola çıktı"));

        // ── GL Hesap Planı ──
        db.Set<ChartOfAccounts>().AddRange(
            ChartOfAccounts.Create(TestTenantId, "100", "Kasa", AcctType.Asset),
            ChartOfAccounts.Create(TestTenantId, "120", "Alıcılar", AcctType.Asset),
            ChartOfAccounts.Create(TestTenantId, "150", "Stoklar", AcctType.Asset),
            ChartOfAccounts.Create(TestTenantId, "320", "Satıcılar", AcctType.Liability),
            ChartOfAccounts.Create(TestTenantId, "391", "Hesaplanan KDV", AcctType.Liability),
            ChartOfAccounts.Create(TestTenantId, "600", "Yurtiçi Satışlar", AcctType.Revenue),
            ChartOfAccounts.Create(TestTenantId, "610", "Satıştan İadeler", AcctType.Revenue),
            ChartOfAccounts.Create(TestTenantId, "760", "Pazarlama Giderleri", AcctType.Expense),
            ChartOfAccounts.Create(TestTenantId, "770", "Genel Yönetim Giderleri", AcctType.Expense),
            ChartOfAccounts.Create(TestTenantId, "689", "Diğer Olağandışı Giderler", AcctType.Expense));

        await db.SaveChangesAsync();
    }

    private static Product CreateProduct(string name, string sku, Guid categoryId, decimal price, int stock)
    {
        return new Product
        {
            TenantId = TestTenantId,
            Name = name,
            SKU = sku,
            CategoryId = categoryId,
            SalePrice = price,
            PurchasePrice = price * 0.6m,
            Stock = stock,
            MinimumStock = 10,
            MaximumStock = 1000,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }

    private static OrderItem CreateOrderItem(Guid orderId, Guid productId, string name, string sku, int qty, decimal unitPrice)
    {
        var item = new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = name,
            ProductSKU = sku,
            TaxRate = 20m
        };
        // internal set properties — reflection ile ata
        typeof(OrderItem).GetProperty("Quantity")!.SetValue(item, qty);
        typeof(OrderItem).GetProperty("UnitPrice")!.SetValue(item, unitPrice);
        typeof(OrderItem).GetProperty("TotalPrice")!.SetValue(item, qty * unitPrice);
        typeof(OrderItem).GetProperty("TaxAmount")!.SetValue(item, qty * unitPrice * 0.20m);
        return item;
    }

    private static StockMovement CreateStockMovement(Guid productId, Guid tenantId, StockMovementType type, int qty, string note)
    {
        return new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            MovementType = type.ToString(),
            Quantity = qty,
            Notes = note,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };
    }
}
