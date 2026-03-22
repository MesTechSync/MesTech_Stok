using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence.Accounting.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// Ahmet Bey Demo Seeder — 14-step end-to-end business flow.
///
/// Proves the entire system works with realistic Turkish data:
///   1. Tenant        2. Store (x2)     3. Category (x3)   4. Product (x5)
///   5. Supplier      6. Order (x3)     7. Invoice (x2)    8. Cargo (x2)
///   9. StockMovement 10. Settlement   11. BankTransaction 12. JournalEntry
///  13. Commission   14. Reconciliation
///
/// Idempotent: checks AhmetBeyTenantId before inserting.
/// </summary>
public class AhmetBeyDemoSeeder
{
    // ── Deterministic GUIDs ──
    public static readonly Guid AhmetBeyTenantId =
        Guid.Parse("AB000000-0000-0000-0000-000000000001");

    private static readonly Guid TrendyolStoreId =
        Guid.Parse("AB000000-0000-0000-0000-000000000010");

    private static readonly Guid HepsiburadaStoreId =
        Guid.Parse("AB000000-0000-0000-0000-000000000011");

    private static readonly Guid CatElektronikId =
        Guid.Parse("AB000000-0000-0000-0000-000000000020");

    private static readonly Guid CatGiyimId =
        Guid.Parse("AB000000-0000-0000-0000-000000000021");

    private static readonly Guid CatEvYasamId =
        Guid.Parse("AB000000-0000-0000-0000-000000000022");

    private static readonly Guid CustomerId =
        Guid.Parse("AB000000-0000-0000-0000-000000000030");

    private static readonly Guid SupplierId =
        Guid.Parse("AB000000-0000-0000-0000-000000000040");

    private static readonly Guid BankAccountId =
        Guid.Parse("AB000000-0000-0000-0000-000000000050");

    private static readonly Guid CustomerAccountId =
        Guid.Parse("AB000000-0000-0000-0000-000000000060");

    // Product GUIDs
    private static readonly Guid ProductBluetoothId =
        Guid.Parse("AB000000-0000-0000-0000-000000000100");

    private static readonly Guid ProductTShirtId =
        Guid.Parse("AB000000-0000-0000-0000-000000000101");

    private static readonly Guid ProductKahveSetId =
        Guid.Parse("AB000000-0000-0000-0000-000000000102");

    private static readonly Guid ProductTabletId =
        Guid.Parse("AB000000-0000-0000-0000-000000000103");

    private static readonly Guid ProductNevresimId =
        Guid.Parse("AB000000-0000-0000-0000-000000000104");

    // Order GUIDs
    private static readonly Guid OrderTrendyolId =
        Guid.Parse("AB000000-0000-0000-0000-000000000200");

    private static readonly Guid OrderHepsiburadaId =
        Guid.Parse("AB000000-0000-0000-0000-000000000201");

    private static readonly Guid OrderManualId =
        Guid.Parse("AB000000-0000-0000-0000-000000000202");

    private readonly AppDbContext _context;
    private readonly ILogger<AhmetBeyDemoSeeder> _logger;

    public AhmetBeyDemoSeeder(AppDbContext context, ILogger<AhmetBeyDemoSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the entire Ahmet Bey 14-step scenario. Idempotent.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var exists = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == AhmetBeyTenantId, ct);

        if (exists)
        {
            _logger.LogInformation("Ahmet Bey demo tenant zaten mevcut, seed atlandi");
            return;
        }

        _logger.LogInformation("Ahmet Bey demo senaryosu baslatiliyor (14 adim)...");

        // Step 1: Tenant
        await Step01_CreateTenantAsync(ct);

        // Step 2: Stores (Trendyol + Hepsiburada)
        await Step02_CreateStoresAsync(ct);

        // Step 3: Categories (Elektronik, Giyim, Ev & Yasam)
        await Step03_CreateCategoriesAsync(ct);

        // Step 4: Products (5 items)
        await Step04_CreateProductsAsync(ct);

        // Step 5: Supplier
        await Step05_CreateSupplierAsync(ct);

        // Step 6: Orders (3 orders: Trendyol, Hepsiburada, Manual)
        await Step06_CreateOrdersAsync(ct);

        // Step 7: Invoices (e-fatura for 2 orders)
        var invoiceIds = await Step07_CreateInvoicesAsync(ct);

        // Step 8: Cargo shipments (for 2 orders)
        await Step08_CreateCargoAsync(ct);

        // Step 9: Stock movements (in/out)
        await Step09_CreateStockMovementsAsync(ct);

        // Step 10: Settlement batch
        var settlementBatchId = await Step10_CreateSettlementAsync(ct);

        // Step 11: Bank transactions (3 transactions)
        var bankTxIds = await Step11_CreateBankTransactionsAsync(ct);

        // Step 12: Journal entries (accounting)
        await Step12_CreateJournalEntriesAsync(ct);

        // Step 13: Commission records
        await Step13_CreateCommissionRecordsAsync(ct);

        // Step 14: Reconciliation
        await Step14_CreateReconciliationAsync(settlementBatchId, bankTxIds, ct);

        _logger.LogInformation(
            "Ahmet Bey demo senaryosu tamamlandi: " +
            "1 tenant, 2 store, 3 category, 5 product, 1 supplier, " +
            "3 order, 2 invoice, 2 cargo, stock movements, " +
            "settlement, 3 bank tx, journal entries, commissions, reconciliation");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 1: TENANT
    // ═══════════════════════════════════════════════════════════

    private async Task Step01_CreateTenantAsync(CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Name = "Ahmet Ticaret A.S.",
            TaxNumber = "4567890123",
            IsActive = true,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(tenant, AhmetBeyTenantId);

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 1/14] Tenant olusturuldu: Ahmet Ticaret A.S. (VKN: 4567890123)");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 2: STORES (Trendyol + Hepsiburada)
    // ═══════════════════════════════════════════════════════════

    private async Task Step02_CreateStoresAsync(CancellationToken ct)
    {
        // Trendyol store
        var trendyolStore = new Store
        {
            TenantId = AhmetBeyTenantId,
            StoreName = "Ahmet Ticaret - Trendyol",
            PlatformType = PlatformType.Trendyol,
            ExternalStoreId = "DEMO-TY-456789",
            IsActive = true,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(trendyolStore, TrendyolStoreId);
        _context.Stores.Add(trendyolStore);

        // Trendyol credentials (placeholder, NOT real)
        var tyCred1 = new StoreCredential
        {
            TenantId = AhmetBeyTenantId,
            StoreId = TrendyolStoreId,
            Key = "ApiKey",
            EncryptedValue = "DEMO-TY-APIKEY-NOT-REAL",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var tyCred2 = new StoreCredential
        {
            TenantId = AhmetBeyTenantId,
            StoreId = TrendyolStoreId,
            Key = "ApiSecret",
            EncryptedValue = "DEMO-TY-SECRET-NOT-REAL",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var tyCred3 = new StoreCredential
        {
            TenantId = AhmetBeyTenantId,
            StoreId = TrendyolStoreId,
            Key = "SellerId",
            EncryptedValue = "DEMO-TY-SELLERID-456789",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        _context.StoreCredentials.Add(tyCred1);
        _context.StoreCredentials.Add(tyCred2);
        _context.StoreCredentials.Add(tyCred3);

        // Hepsiburada store
        var hbStore = new Store
        {
            TenantId = AhmetBeyTenantId,
            StoreName = "Ahmet Ticaret - Hepsiburada",
            PlatformType = PlatformType.Hepsiburada,
            ExternalStoreId = "DEMO-HB-123456",
            IsActive = true,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(hbStore, HepsiburadaStoreId);
        _context.Stores.Add(hbStore);

        // Hepsiburada credentials
        var hbCred1 = new StoreCredential
        {
            TenantId = AhmetBeyTenantId,
            StoreId = HepsiburadaStoreId,
            Key = "MerchantId",
            EncryptedValue = "DEMO-HB-MERCHANT-NOT-REAL",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var hbCred2 = new StoreCredential
        {
            TenantId = AhmetBeyTenantId,
            StoreId = HepsiburadaStoreId,
            Key = "ApiKey",
            EncryptedValue = "DEMO-HB-APIKEY-NOT-REAL",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        _context.StoreCredentials.Add(hbCred1);
        _context.StoreCredentials.Add(hbCred2);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 2/14] 2 magaza olusturuldu: Trendyol + Hepsiburada (demo credentials)");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 3: CATEGORIES
    // ═══════════════════════════════════════════════════════════

    private async Task Step03_CreateCategoriesAsync(CancellationToken ct)
    {
        var categories = new (Guid Id, string Name, string Code, string Desc, int Sort)[]
        {
            (CatElektronikId, "Elektronik", "ELK", "Elektronik urunler, aksesuarlar", 1),
            (CatGiyimId, "Giyim", "GYM", "Kadin, erkek, cocuk giyim", 2),
            (CatEvYasamId, "Ev & Yasam", "EVY", "Ev dekorasyon, mutfak, tekstil", 3),
        };

        foreach (var (id, name, code, desc, sort) in categories)
        {
            var cat = new Category
            {
                TenantId = AhmetBeyTenantId,
                Name = name,
                Code = code,
                Description = desc,
                SortOrder = sort,
                IsActive = true,
                ShowInMenu = true,
                CreatedBy = "AhmetBeyDemoSeeder"
            };
            SetEntityId(cat, id);
            _context.Categories.Add(cat);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 3/14] 3 kategori olusturuldu: Elektronik, Giyim, Ev & Yasam");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 4: PRODUCTS (5 items)
    // ═══════════════════════════════════════════════════════════

    private async Task Step04_CreateProductsAsync(CancellationToken ct)
    {
        var products = new[]
        {
            new
            {
                Id = ProductBluetoothId,
                Name = "Bluetooth Kulaklik TWS-500 Pro",
                SKU = "AB-ELK-001",
                Barcode = "8690123456001",
                PurchasePrice = 180.00m,
                SalePrice = 499.90m,
                Stock = 75,
                Brand = "TechSound",
                CategoryId = CatElektronikId,
                Weight = 0.12m
            },
            new
            {
                Id = ProductTShirtId,
                Name = "Pamuklu Oversize T-Shirt Beyaz",
                SKU = "AB-GYM-001",
                Barcode = "8690123456002",
                PurchasePrice = 45.00m,
                SalePrice = 149.90m,
                Stock = 200,
                Brand = "Ahmet Moda",
                CategoryId = CatGiyimId,
                Weight = 0.25m
            },
            new
            {
                Id = ProductKahveSetId,
                Name = "Porselen Turk Kahvesi Fincan Seti 6'li",
                SKU = "AB-EVY-001",
                Barcode = "8690123456003",
                PurchasePrice = 85.00m,
                SalePrice = 249.90m,
                Stock = 50,
                Brand = "Porselen Art",
                CategoryId = CatEvYasamId,
                Weight = 1.20m
            },
            new
            {
                Id = ProductTabletId,
                Name = "10.1 inc Android Tablet 64GB",
                SKU = "AB-ELK-002",
                Barcode = "8690123456004",
                PurchasePrice = 1200.00m,
                SalePrice = 2999.90m,
                Stock = 25,
                Brand = "SmartTab",
                CategoryId = CatElektronikId,
                Weight = 0.48m
            },
            new
            {
                Id = ProductNevresimId,
                Name = "Ranforce Cift Kisilik Nevresim Takimi",
                SKU = "AB-EVY-002",
                Barcode = "8690123456005",
                PurchasePrice = 150.00m,
                SalePrice = 399.90m,
                Stock = 80,
                Brand = "Ev Tekstil",
                CategoryId = CatEvYasamId,
                Weight = 2.10m
            },
        };

        foreach (var p in products)
        {
            var product = new Product
            {
                TenantId = AhmetBeyTenantId,
                Name = p.Name,
                SKU = p.SKU,
                Barcode = p.Barcode,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice,
                ListPrice = p.SalePrice * 1.25m, // Trendyol liste fiyati ~%25 yukarda
                Stock = p.Stock,
                MinimumStock = 5,
                MaximumStock = 500,
                ReorderLevel = 10,
                ReorderQuantity = 50,
                CategoryId = p.CategoryId,
                Brand = p.Brand,
                TaxRate = 0.20m, // %20 KDV
                CurrencyCode = "TRY",
                Weight = p.Weight,
                WeightUnit = WeightUnit.Kilogram,
                IsActive = true,
                CreatedBy = "AhmetBeyDemoSeeder"
            };
            SetEntityId(product, p.Id);
            _context.Products.Add(product);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 4/14] 5 urun olusturuldu (KDV %20, TRY fiyatlar)");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 5: SUPPLIER
    // ═══════════════════════════════════════════════════════════

    private async Task Step05_CreateSupplierAsync(CancellationToken ct)
    {
        var supplier = new Supplier
        {
            TenantId = AhmetBeyTenantId,
            Name = "Guney Elektronik Tic. Ltd. Sti.",
            Code = "SUP-001",
            Description = "Elektronik urun tedarikci (dropshipping)",
            ContactPerson = "Mehmet Guney",
            Email = "mehmet@guneyelectronik.com.tr",
            Phone = "+90 212 555 7890",
            Mobile = "+90 532 555 7891",
            Address = "Bayrampasa Sanayi Sitesi No:45, Bayrampasa",
            City = "Istanbul",
            Country = "TR",
            TaxNumber = "7890123456",
            TaxOffice = "Bayrampasa VD",
            PaymentTermDays = 30,
            CreditLimit = 100000.00m,
            Currency = "TRY",
            IsActive = true,
            Notes = "Dropshipping anlasmasi mevcut. Elektronik kategorisinde ana tedarikci.",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(supplier, SupplierId);
        supplier.MarkAsPreferred();
        supplier.SetRating(4);
        _context.Suppliers.Add(supplier);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 5/14] 1 tedarikci olusturuldu: Guney Elektronik (dropshipping)");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 6: ORDERS (3 orders)
    // ═══════════════════════════════════════════════════════════

    private async Task Step06_CreateOrdersAsync(CancellationToken ct)
    {
        // First, create the customer
        var customer = new Customer
        {
            TenantId = AhmetBeyTenantId,
            Name = "Zeynep Kara",
            Code = "MUS-001",
            CustomerType = "INDIVIDUAL",
            ContactPerson = "Zeynep Kara",
            Email = "zeynep.kara@email.com",
            Phone = "+90 535 555 1234",
            City = "Ankara",
            Country = "TR",
            IsActive = true,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(customer, CustomerId);
        _context.Customers.Add(customer);

        // Create CustomerAccount for cari hesap tracking
        var customerAccount = new CustomerAccount
        {
            TenantId = AhmetBeyTenantId,
            CustomerId = CustomerId,
            AccountCode = "120.MUS-001",
            CustomerName = "Zeynep Kara",
            CustomerEmail = "zeynep.kara@email.com",
            CustomerPhone = "+90 535 555 1234",
            Currency = "TRY",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        customerAccount.SetCreditLimit(10000.00m);
        customerAccount.Activate();
        SetEntityId(customerAccount, CustomerAccountId);
        _context.CustomerAccounts.Add(customerAccount);

        await _context.SaveChangesAsync(ct);

        // ── ORDER 1: Trendyol (Delivered) ──
        var order1 = new Order
        {
            TenantId = AhmetBeyTenantId,
            OrderNumber = "AB-TY-2026-001",
            CustomerId = CustomerId,
            OrderDate = DateTime.UtcNow.AddDays(-10),
            SourcePlatform = PlatformType.Trendyol,
            ExternalOrderId = "TY-98765432",
            PlatformOrderNumber = "98765432",
            CustomerName = "Zeynep Kara",
            CustomerEmail = "zeynep.kara@email.com",
            PaymentStatus = "Paid",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(order1, OrderTrendyolId);

        // Order 1 items: 1x Bluetooth Kulaklik + 2x T-Shirt
        var o1Item1 = new OrderItem
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderTrendyolId,
            ProductId = ProductBluetoothId,
            ProductName = "Bluetooth Kulaklik TWS-500 Pro",
            ProductSKU = "AB-ELK-001",
            Quantity = 1,
            UnitPrice = 499.90m,
            TotalPrice = 499.90m,
            TaxRate = 0.20m,
            TaxAmount = 99.98m,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var o1Item2 = new OrderItem
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderTrendyolId,
            ProductId = ProductTShirtId,
            ProductName = "Pamuklu Oversize T-Shirt Beyaz",
            ProductSKU = "AB-GYM-001",
            Quantity = 2,
            UnitPrice = 149.90m,
            TotalPrice = 299.80m,
            TaxRate = 0.20m,
            TaxAmount = 59.96m,
            CreatedBy = "AhmetBeyDemoSeeder"
        };

        order1.SetFinancials(799.70m, 159.94m, 959.64m);
        order1.TaxRate = 0.20m;
        order1.SetCommission(12.99m, Math.Round(799.70m * 12.99m / 100m, 2)); // 103.88 TL
        order1.SetCargoExpense(24.99m);

        // State transitions: Pending → Confirmed → Shipped → Delivered
        order1.Place();
        order1.MarkAsShipped("YK-2026-001234", CargoProvider.YurticiKargo);
        order1.MarkAsDelivered();

        _context.Orders.Add(order1);
        _context.OrderItems.Add(o1Item1);
        _context.OrderItems.Add(o1Item2);

        // ── ORDER 2: Hepsiburada (Shipped) ──
        var order2 = new Order
        {
            TenantId = AhmetBeyTenantId,
            OrderNumber = "AB-HB-2026-001",
            CustomerId = CustomerId,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            SourcePlatform = PlatformType.Hepsiburada,
            ExternalOrderId = "HB-55443322",
            PlatformOrderNumber = "55443322",
            CustomerName = "Zeynep Kara",
            CustomerEmail = "zeynep.kara@email.com",
            PaymentStatus = "Paid",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(order2, OrderHepsiburadaId);

        // Order 2 items: 1x Tablet
        var o2Item1 = new OrderItem
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderHepsiburadaId,
            ProductId = ProductTabletId,
            ProductName = "10.1 inc Android Tablet 64GB",
            ProductSKU = "AB-ELK-002",
            Quantity = 1,
            UnitPrice = 2999.90m,
            TotalPrice = 2999.90m,
            TaxRate = 0.20m,
            TaxAmount = 599.98m,
            CreatedBy = "AhmetBeyDemoSeeder"
        };

        order2.SetFinancials(2999.90m, 599.98m, 3599.88m);
        order2.TaxRate = 0.20m;
        order2.SetCommission(14.50m, Math.Round(2999.90m * 14.50m / 100m, 2)); // 434.99 TL
        order2.SetCargoExpense(34.99m);

        // State transitions: Pending → Confirmed → Shipped
        order2.Place();
        order2.MarkAsShipped("AK-2026-005678", CargoProvider.ArasKargo);

        _context.Orders.Add(order2);
        _context.OrderItems.Add(o2Item1);

        // ── ORDER 3: Manual (Confirmed — not yet shipped) ──
        var order3 = new Order
        {
            TenantId = AhmetBeyTenantId,
            OrderNumber = "AB-MAN-2026-001",
            CustomerId = CustomerId,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            CustomerName = "Zeynep Kara",
            CustomerEmail = "zeynep.kara@email.com",
            PaymentStatus = "Paid",
            Notes = "Manuel siparis — web sitesi uzerinden",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        SetEntityId(order3, OrderManualId);

        // Order 3 items: 1x Kahve Seti + 1x Nevresim
        var o3Item1 = new OrderItem
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderManualId,
            ProductId = ProductKahveSetId,
            ProductName = "Porselen Turk Kahvesi Fincan Seti 6'li",
            ProductSKU = "AB-EVY-001",
            Quantity = 1,
            UnitPrice = 249.90m,
            TotalPrice = 249.90m,
            TaxRate = 0.20m,
            TaxAmount = 49.98m,
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var o3Item2 = new OrderItem
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderManualId,
            ProductId = ProductNevresimId,
            ProductName = "Ranforce Cift Kisilik Nevresim Takimi",
            ProductSKU = "AB-EVY-002",
            Quantity = 1,
            UnitPrice = 399.90m,
            TotalPrice = 399.90m,
            TaxRate = 0.20m,
            TaxAmount = 79.98m,
            CreatedBy = "AhmetBeyDemoSeeder"
        };

        order3.SetFinancials(649.80m, 129.96m, 779.76m);
        order3.TaxRate = 0.20m;

        // State transition: Pending → Confirmed
        order3.Place();

        _context.Orders.Add(order3);
        _context.OrderItems.Add(o3Item1);
        _context.OrderItems.Add(o3Item2);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 6/14] 3 siparis olusturuldu: " +
            "TY={Total1:N2} TL, HB={Total2:N2} TL, Manuel={Total3:N2} TL",
            order1.TotalAmount, order2.TotalAmount, order3.TotalAmount);
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 7: INVOICES (e-fatura for orders 1 & 2)
    // ═══════════════════════════════════════════════════════════

    private async Task<(Guid Invoice1Id, Guid Invoice2Id)> Step07_CreateInvoicesAsync(CancellationToken ct)
    {
        // Invoice for Order 1 (Trendyol — Delivered)
        var inv1 = new Invoice
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderTrendyolId,
            StoreId = TrendyolStoreId,
            InvoiceNumber = "AB2026000001",
            Type = InvoiceType.EArsiv, // Bireysel musteri -> e-Arsiv
            Direction = InvoiceDirection.Outgoing,
            Provider = InvoiceProvider.Sovos,
            CustomerName = "Zeynep Kara",
            CustomerAddress = "Cankaya, Ankara",
            CustomerEmail = "zeynep.kara@email.com",
            IsEInvoiceTaxpayer = false,
            Currency = "TRY",
            PlatformCode = "TY",
            PlatformOrderId = "TY-98765432",
            InvoiceDate = DateTime.UtcNow.AddDays(-9),
            GLAccountCode = "600.01.001", // Trendyol Satislari
            CreatedBy = "AhmetBeyDemoSeeder"
        };

        // Invoice lines for Order 1 — AddLine auto-calculates totals
        inv1.AddLine(new InvoiceLine
        {
            TenantId = AhmetBeyTenantId,
            InvoiceId = inv1.Id,
            ProductId = ProductBluetoothId,
            ProductName = "Bluetooth Kulaklik TWS-500 Pro",
            SKU = "AB-ELK-001",
            Barcode = "8690123456001",
            Quantity = 1,
            UnitPrice = 499.90m,
            TaxRate = 0.20m,
            TaxAmount = 99.98m,
            LineTotal = 599.88m,
            CreatedBy = "AhmetBeyDemoSeeder"
        });
        inv1.AddLine(new InvoiceLine
        {
            TenantId = AhmetBeyTenantId,
            InvoiceId = inv1.Id,
            ProductId = ProductTShirtId,
            ProductName = "Pamuklu Oversize T-Shirt Beyaz",
            SKU = "AB-GYM-001",
            Barcode = "8690123456002",
            Quantity = 2,
            UnitPrice = 149.90m,
            TaxRate = 0.20m,
            TaxAmount = 59.96m,
            LineTotal = 359.76m,
            CreatedBy = "AhmetBeyDemoSeeder"
        });
        inv1.MarkAsSent("DEMO-GIB-00000001", null);

        _context.Invoices.Add(inv1);

        // Invoice for Order 2 (Hepsiburada — Shipped)
        var inv2 = new Invoice
        {
            TenantId = AhmetBeyTenantId,
            OrderId = OrderHepsiburadaId,
            StoreId = HepsiburadaStoreId,
            InvoiceNumber = "AB2026000002",
            Type = InvoiceType.EArsiv,
            Direction = InvoiceDirection.Outgoing,
            Provider = InvoiceProvider.Sovos,
            CustomerName = "Zeynep Kara",
            CustomerAddress = "Cankaya, Ankara",
            CustomerEmail = "zeynep.kara@email.com",
            IsEInvoiceTaxpayer = false,
            Currency = "TRY",
            PlatformCode = "HB",
            PlatformOrderId = "HB-55443322",
            InvoiceDate = DateTime.UtcNow.AddDays(-4),
            GLAccountCode = "600.01.002", // Hepsiburada Satislari
            CreatedBy = "AhmetBeyDemoSeeder"
        };

        inv2.AddLine(new InvoiceLine
        {
            TenantId = AhmetBeyTenantId,
            InvoiceId = inv2.Id,
            ProductId = ProductTabletId,
            ProductName = "10.1 inc Android Tablet 64GB",
            SKU = "AB-ELK-002",
            Barcode = "8690123456004",
            Quantity = 1,
            UnitPrice = 2999.90m,
            TaxRate = 0.20m,
            TaxAmount = 599.98m,
            LineTotal = 3599.88m,
            CreatedBy = "AhmetBeyDemoSeeder"
        });
        inv2.MarkAsSent("DEMO-GIB-00000002", null);

        _context.Invoices.Add(inv2);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 7/14] 2 e-fatura olusturuldu: {Inv1}={Total1:N2} TL, {Inv2}={Total2:N2} TL",
            inv1.InvoiceNumber, inv1.GrandTotal,
            inv2.InvoiceNumber, inv2.GrandTotal);

        return (inv1.Id, inv2.Id);
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 8: CARGO (shipments for orders 1 & 2)
    // ═══════════════════════════════════════════════════════════

    private async Task Step08_CreateCargoAsync(CancellationToken ct)
    {
        // Cargo expense for Order 1 (Yurtici Kargo)
        var cargo1 = CargoExpense.Create(
            tenantId: AhmetBeyTenantId,
            carrierName: "Yurtici Kargo",
            cost: 24.99m,
            orderId: OrderTrendyolId.ToString(),
            trackingNumber: "YK-2026-001234");
        _context.CargoExpenses.Add(cargo1);

        // Cargo expense for Order 2 (Aras Kargo)
        var cargo2 = CargoExpense.Create(
            tenantId: AhmetBeyTenantId,
            carrierName: "Aras Kargo",
            cost: 34.99m,
            orderId: OrderHepsiburadaId.ToString(),
            trackingNumber: "AK-2026-005678");
        _context.CargoExpenses.Add(cargo2);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 8/14] 2 kargo kaydi olusturuldu: YK=24.99 TL, AK=34.99 TL");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 9: STOCK MOVEMENTS
    // ═══════════════════════════════════════════════════════════

    private async Task Step09_CreateStockMovementsAsync(CancellationToken ct)
    {
        var movements = new[]
        {
            // Supplier purchase — stock in for all 5 products
            new { ProductId = ProductBluetoothId, Qty = 100, Prev = 0, New = 100, Type = "Purchase", Reason = "Ilk tedarik — Guney Elektronik", OrderId = (Guid?)null, SuppId = (Guid?)SupplierId, Cost = 180.00m },
            new { ProductId = ProductTShirtId, Qty = 250, Prev = 0, New = 250, Type = "Purchase", Reason = "Ilk tedarik — Ahmet Moda atolyesi", OrderId = (Guid?)null, SuppId = (Guid?)null, Cost = 45.00m },
            new { ProductId = ProductKahveSetId, Qty = 60, Prev = 0, New = 60, Type = "Purchase", Reason = "Ilk tedarik — Porselen Art", OrderId = (Guid?)null, SuppId = (Guid?)null, Cost = 85.00m },
            new { ProductId = ProductTabletId, Qty = 30, Prev = 0, New = 30, Type = "Purchase", Reason = "Ilk tedarik — Guney Elektronik", OrderId = (Guid?)null, SuppId = (Guid?)SupplierId, Cost = 1200.00m },
            new { ProductId = ProductNevresimId, Qty = 100, Prev = 0, New = 100, Type = "Purchase", Reason = "Ilk tedarik — Ev Tekstil", OrderId = (Guid?)null, SuppId = (Guid?)null, Cost = 150.00m },

            // Sales out — Order 1 (Trendyol)
            new { ProductId = ProductBluetoothId, Qty = -1, Prev = 100, New = 99, Type = "PlatformSale", Reason = "Trendyol siparis AB-TY-2026-001", OrderId = (Guid?)OrderTrendyolId, SuppId = (Guid?)null, Cost = 180.00m },
            new { ProductId = ProductTShirtId, Qty = -2, Prev = 250, New = 248, Type = "PlatformSale", Reason = "Trendyol siparis AB-TY-2026-001", OrderId = (Guid?)OrderTrendyolId, SuppId = (Guid?)null, Cost = 45.00m },

            // Sales out — Order 2 (Hepsiburada)
            new { ProductId = ProductTabletId, Qty = -1, Prev = 30, New = 29, Type = "PlatformSale", Reason = "Hepsiburada siparis AB-HB-2026-001", OrderId = (Guid?)OrderHepsiburadaId, SuppId = (Guid?)null, Cost = 1200.00m },

            // Adjustment — inventory count correction
            new { ProductId = ProductNevresimId, Qty = -2, Prev = 100, New = 98, Type = "Adjustment", Reason = "Sayim farki — 2 adet fire", OrderId = (Guid?)null, SuppId = (Guid?)null, Cost = 150.00m },
        };

        foreach (var m in movements)
        {
            var sm = new StockMovement
            {
                TenantId = AhmetBeyTenantId,
                ProductId = m.ProductId,
                Quantity = m.Qty,
                MovementType = m.Type,
                Reason = m.Reason,
                OrderId = m.OrderId,
                SupplierId = m.SuppId,
                UnitCost = m.Cost,
                TotalCost = Math.Abs(m.Qty) * m.Cost,
                Date = DateTime.UtcNow.AddDays(m.OrderId.HasValue ? -10 : -15),
                ProcessedBy = "AhmetBeyDemoSeeder",
                CreatedBy = "AhmetBeyDemoSeeder"
            };
            sm.SetStockLevels(m.Prev, m.New);
            sm.Approve("AhmetBeyDemoSeeder");
            _context.StockMovements.Add(sm);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 9/14] {Count} stok hareketi olusturuldu (5 giris + 3 cikis + 1 duzeltme)", movements.Length);
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 10: SETTLEMENT BATCH
    // ═══════════════════════════════════════════════════════════

    private async Task<Guid> Step10_CreateSettlementAsync(CancellationToken ct)
    {
        // Trendyol weekly settlement period
        var periodStart = DateTime.UtcNow.AddDays(-14);
        var periodEnd = DateTime.UtcNow.AddDays(-7);

        // Order 1 amounts: Gross=799.70, Commission=103.88, Cargo=24.99
        decimal order1Gross = 799.70m;
        decimal order1Commission = Math.Round(order1Gross * 12.99m / 100m, 2); // 103.88
        decimal order1ServiceFee = 3.99m;
        decimal order1Cargo = 24.99m;
        decimal order1Net = order1Gross - order1Commission - order1ServiceFee - order1Cargo; // 666.84

        var batch = SettlementBatch.Create(
            tenantId: AhmetBeyTenantId,
            platform: "Trendyol",
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalGross: order1Gross,
            totalCommission: order1Commission,
            totalNet: order1Net);

        _context.SettlementBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        // Settlement lines (order-level detail)
        var line = SettlementLine.Create(
            tenantId: AhmetBeyTenantId,
            settlementBatchId: batch.Id,
            orderId: OrderTrendyolId.ToString(),
            grossAmount: order1Gross,
            commissionAmount: order1Commission,
            serviceFee: order1ServiceFee,
            cargoDeduction: order1Cargo,
            refundDeduction: 0m,
            netAmount: order1Net);

        _context.SettlementLines.Add(line);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 10/14] Settlement batch olusturuldu: Trendyol donem {Start:dd.MM}-{End:dd.MM}, " +
            "Brut={Gross:N2} TL, Komisyon={Comm:N2} TL, Net={Net:N2} TL",
            periodStart, periodEnd, order1Gross, order1Commission, order1Net);

        return batch.Id;
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 11: BANK TRANSACTIONS (3 entries)
    // ═══════════════════════════════════════════════════════════

    private async Task<List<Guid>> Step11_CreateBankTransactionsAsync(CancellationToken ct)
    {
        // First, create a bank account
        var bankAccount = BankAccount.Create(
            tenantId: AhmetBeyTenantId,
            accountName: "Ahmet Ticaret - Isbank Vadesiz",
            currency: "TRY",
            bankName: "Turkiye Is Bankasi",
            iban: "TR33 0006 4000 0011 2345 6789 00",
            accountNumber: "1234567890",
            isDefault: true);

        // Use reflection to set the deterministic ID
        typeof(BankAccount).GetProperty(nameof(BankAccount.Id))!.SetValue(bankAccount, BankAccountId);
        _context.BankAccounts.Add(bankAccount);
        await _context.SaveChangesAsync(ct);

        var txIds = new List<Guid>();

        // Transaction 1: Trendyol settlement payment received
        var tx1 = BankTransaction.Create(
            tenantId: AhmetBeyTenantId,
            bankAccountId: BankAccountId,
            transactionDate: DateTime.UtcNow.AddDays(-5),
            amount: 666.84m, // Net from settlement
            description: "TRENDYOL A.S. HESAP KESIMI ODEMESI",
            referenceNumber: "TY-PAY-2026-W10",
            idempotencyKey: "AB-DEMO-BANK-TX-001");
        _context.AccountingBankTransactions.Add(tx1);
        txIds.Add(tx1.Id);

        // Transaction 2: Hepsiburada settlement payment received
        decimal hbNetAmount = 2999.90m - Math.Round(2999.90m * 14.50m / 100m, 2) - 5.99m - 34.99m;
        var tx2 = BankTransaction.Create(
            tenantId: AhmetBeyTenantId,
            bankAccountId: BankAccountId,
            transactionDate: DateTime.UtcNow.AddDays(-3),
            amount: hbNetAmount,
            description: "D-MARKET ELEKTRONIK HIZM. HESAP KESIMI",
            referenceNumber: "HB-PAY-2026-B05",
            idempotencyKey: "AB-DEMO-BANK-TX-002");
        _context.AccountingBankTransactions.Add(tx2);
        txIds.Add(tx2.Id);

        // Transaction 3: Supplier payment (outgoing)
        var tx3 = BankTransaction.Create(
            tenantId: AhmetBeyTenantId,
            bankAccountId: BankAccountId,
            transactionDate: DateTime.UtcNow.AddDays(-2),
            amount: -5400.00m, // Negative = outgoing payment
            description: "GUNEY ELEKTRONIK LTD. STI. TEDARIKCI ODEMESI",
            referenceNumber: "EFT-2026-00123",
            idempotencyKey: "AB-DEMO-BANK-TX-003");
        _context.AccountingBankTransactions.Add(tx3);
        txIds.Add(tx3.Id);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 11/14] 3 banka hareketi olusturuldu: " +
            "TY odeme=+{TyAmount:N2}, HB odeme=+{HbAmount:N2}, Tedarikci=-5400.00",
            666.84m, hbNetAmount);

        return txIds;
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 12: JOURNAL ENTRIES (accounting)
    // ═══════════════════════════════════════════════════════════

    private async Task Step12_CreateJournalEntriesAsync(CancellationToken ct)
    {
        // First, seed chart of accounts for this tenant
        var existingAccounts = await _context.ChartOfAccounts
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == AhmetBeyTenantId)
            .ToListAsync(ct);

        IReadOnlyList<ChartOfAccounts> accounts;
        if (existingAccounts.Count == 0)
        {
            var seedAccounts = ChartOfAccountsSeedData.GetDefaultAccounts(AhmetBeyTenantId);
            _context.ChartOfAccounts.AddRange(seedAccounts);
            await _context.SaveChangesAsync(ct);
            accounts = seedAccounts;
        }
        else
        {
            accounts = existingAccounts;
        }

        // Lookup account IDs by code
        Guid AccountId(string code) => accounts.First(a => a.Code == code).Id;

        // ── Journal Entry 1: Trendyol sale (Order 1) ──
        // Borc: 120.01.001 Trendyol Alacak = 959.64 (KDV dahil toplam)
        // Alacak: 600.01.001 Trendyol Satislari = 799.70 (net satis)
        // Alacak: 391 Hesaplanan KDV = 159.94
        var je1 = JournalEntry.Create(
            tenantId: AhmetBeyTenantId,
            entryDate: DateTime.UtcNow.AddDays(-10),
            description: "Trendyol siparis AB-TY-2026-001 satis kaydı",
            referenceNumber: "AB2026000001");

        je1.AddLine(AccountId("120.01.001"), debit: 959.64m, credit: 0m, description: "Trendyol alacak");
        je1.AddLine(AccountId("600.01.001"), debit: 0m, credit: 799.70m, description: "Trendyol satis geliri");
        je1.AddLine(AccountId("391"), debit: 0m, credit: 159.94m, description: "Hesaplanan KDV %20");

        _context.JournalEntries.Add(je1);

        // ── Journal Entry 2: Trendyol commission expense ──
        // Borc: 631.01.001 Trendyol Komisyon = 103.88
        // Alacak: 120.01.001 Trendyol Alacak = 103.88
        var je2 = JournalEntry.Create(
            tenantId: AhmetBeyTenantId,
            entryDate: DateTime.UtcNow.AddDays(-10),
            description: "Trendyol komisyon gideri AB-TY-2026-001",
            referenceNumber: "KOM-AB-TY-001");

        decimal tyCommission = Math.Round(799.70m * 12.99m / 100m, 2);
        je2.AddLine(AccountId("631.01.001"), debit: tyCommission, credit: 0m, description: "Trendyol platform komisyonu");
        je2.AddLine(AccountId("120.01.001"), debit: 0m, credit: tyCommission, description: "Komisyon mahsubu");

        _context.JournalEntries.Add(je2);

        // ── Journal Entry 3: Hepsiburada sale (Order 2) ──
        var je3 = JournalEntry.Create(
            tenantId: AhmetBeyTenantId,
            entryDate: DateTime.UtcNow.AddDays(-5),
            description: "Hepsiburada siparis AB-HB-2026-001 satis kaydı",
            referenceNumber: "AB2026000002");

        je3.AddLine(AccountId("120.01.002"), debit: 3599.88m, credit: 0m, description: "Hepsiburada alacak");
        je3.AddLine(AccountId("600.01.002"), debit: 0m, credit: 2999.90m, description: "Hepsiburada satis geliri");
        je3.AddLine(AccountId("391"), debit: 0m, credit: 599.98m, description: "Hesaplanan KDV %20");

        _context.JournalEntries.Add(je3);

        // ── Journal Entry 4: Hepsiburada commission expense ──
        var je4 = JournalEntry.Create(
            tenantId: AhmetBeyTenantId,
            entryDate: DateTime.UtcNow.AddDays(-5),
            description: "Hepsiburada komisyon gideri AB-HB-2026-001",
            referenceNumber: "KOM-AB-HB-001");

        decimal hbCommission = Math.Round(2999.90m * 14.50m / 100m, 2);
        je4.AddLine(AccountId("631.01.002"), debit: hbCommission, credit: 0m, description: "Hepsiburada platform komisyonu");
        je4.AddLine(AccountId("120.01.002"), debit: 0m, credit: hbCommission, description: "Komisyon mahsubu");

        _context.JournalEntries.Add(je4);

        // ── Journal Entry 5: Bank receipt — Trendyol payment ──
        var je5 = JournalEntry.Create(
            tenantId: AhmetBeyTenantId,
            entryDate: DateTime.UtcNow.AddDays(-5),
            description: "Trendyol hesap kesimi banka odemesi",
            referenceNumber: "TY-PAY-2026-W10");

        // Net received after commission + service fee + cargo
        decimal tyNetReceived = 799.70m - tyCommission - 3.99m - 24.99m;
        je5.AddLine(AccountId("102.01"), debit: tyNetReceived, credit: 0m, description: "Banka havalesi — Trendyol");
        je5.AddLine(AccountId("631.02"), debit: 24.99m, credit: 0m, description: "Kargo gideri");
        // Service fee goes to general selling expense
        je5.AddLine(AccountId("631.01.001"), debit: 3.99m, credit: 0m, description: "Platform hizmet bedeli");
        je5.AddLine(AccountId("120.01.001"), debit: 0m, credit: tyNetReceived + 24.99m + 3.99m, description: "Trendyol alacak kapanisi");

        _context.JournalEntries.Add(je5);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[Step 12/14] 5 yevmiye kaydi olusturuldu (satis, komisyon, banka tahsilat)");
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 13: COMMISSION RECORDS
    // ═══════════════════════════════════════════════════════════

    private async Task Step13_CreateCommissionRecordsAsync(CancellationToken ct)
    {
        // Trendyol commission for Order 1
        var comm1 = CommissionRecord.Create(
            tenantId: AhmetBeyTenantId,
            platform: "Trendyol",
            grossAmount: 799.70m,
            commissionRate: 12.99m,
            commissionAmount: Math.Round(799.70m * 12.99m / 100m, 2),
            serviceFee: 3.99m,
            orderId: OrderTrendyolId.ToString(),
            category: "Elektronik + Giyim",
            commissionType: CommissionType.Percentage,
            rateSource: "Trendyol Seller Center — 2026 Q1 tarife");
        _context.CommissionRecords.Add(comm1);

        // Hepsiburada commission for Order 2
        var comm2 = CommissionRecord.Create(
            tenantId: AhmetBeyTenantId,
            platform: "Hepsiburada",
            grossAmount: 2999.90m,
            commissionRate: 14.50m,
            commissionAmount: Math.Round(2999.90m * 14.50m / 100m, 2),
            serviceFee: 5.99m,
            orderId: OrderHepsiburadaId.ToString(),
            category: "Elektronik",
            commissionType: CommissionType.Percentage,
            rateSource: "Hepsiburada Merchant Panel — 2026 Q1 tarife");
        _context.CommissionRecords.Add(comm2);

        // Also store PlatformCommission rate definitions
        var pc1 = new PlatformCommission
        {
            TenantId = AhmetBeyTenantId,
            Platform = PlatformType.Trendyol,
            Type = CommissionType.Percentage,
            CategoryName = "Elektronik",
            Rate = 12.99m,
            Currency = "TRY",
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            Notes = "Trendyol 2026 Q1 Elektronik kategorisi komisyon oranı",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var pc2 = new PlatformCommission
        {
            TenantId = AhmetBeyTenantId,
            Platform = PlatformType.Trendyol,
            Type = CommissionType.Percentage,
            CategoryName = "Giyim",
            Rate = 15.99m,
            Currency = "TRY",
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            Notes = "Trendyol 2026 Q1 Giyim kategorisi komisyon oranı",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        var pc3 = new PlatformCommission
        {
            TenantId = AhmetBeyTenantId,
            Platform = PlatformType.Hepsiburada,
            Type = CommissionType.Percentage,
            CategoryName = "Elektronik",
            Rate = 14.50m,
            Currency = "TRY",
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            Notes = "Hepsiburada 2026 Q1 Elektronik kategorisi komisyon oranı",
            CreatedBy = "AhmetBeyDemoSeeder"
        };
        _context.PlatformCommissions.Add(pc1);
        _context.PlatformCommissions.Add(pc2);
        _context.PlatformCommissions.Add(pc3);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 13/14] Komisyon kayitlari olusturuldu: " +
            "TY %12.99={TyComm:N2} TL, HB %14.50={HbComm:N2} TL",
            Math.Round(799.70m * 12.99m / 100m, 2),
            Math.Round(2999.90m * 14.50m / 100m, 2));
    }

    // ═══════════════════════════════════════════════════════════
    // STEP 14: RECONCILIATION
    // ═══════════════════════════════════════════════════════════

    private async Task Step14_CreateReconciliationAsync(
        Guid settlementBatchId,
        List<Guid> bankTxIds,
        CancellationToken ct)
    {
        // Match: Settlement batch <-> Bank transaction 1 (Trendyol payment)
        var match1 = ReconciliationMatch.Create(
            tenantId: AhmetBeyTenantId,
            matchDate: DateTime.UtcNow,
            confidence: 0.98m, // High confidence — amounts match
            status: ReconciliationStatus.AutoMatched,
            settlementBatchId: settlementBatchId,
            bankTransactionId: bankTxIds[0]);
        _context.ReconciliationMatches.Add(match1);

        // Bank tx 2 (Hepsiburada) — no settlement batch yet → NeedsReview
        var match2 = ReconciliationMatch.Create(
            tenantId: AhmetBeyTenantId,
            matchDate: DateTime.UtcNow,
            confidence: 0.65m,
            status: ReconciliationStatus.NeedsReview,
            bankTransactionId: bankTxIds[1]);
        _context.ReconciliationMatches.Add(match2);

        // Bank tx 3 (Supplier payment) — no settlement → NeedsReview
        var match3 = ReconciliationMatch.Create(
            tenantId: AhmetBeyTenantId,
            matchDate: DateTime.UtcNow,
            confidence: 0.50m,
            status: ReconciliationStatus.NeedsReview,
            bankTransactionId: bankTxIds[2]);
        _context.ReconciliationMatches.Add(match3);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[Step 14/14] Mutabakat tamamlandi: 1 AutoMatched (%98), 2 NeedsReview");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private static void SetEntityId(object entity, Guid id)
    {
        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
            .SetValue(entity, id);
    }
}
