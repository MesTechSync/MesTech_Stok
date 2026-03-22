using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using AccountingAccountType = MesTech.Domain.Accounting.Enums.AccountType;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// "Ahmet Bey" E2E Testcontainers senaryosu — 14 adim gercek PostgreSQL.
///
/// Kozmetik magazasi sahibi Ahmet Bey'in MesTech platformunu kullanarak
/// gunluk is akisini uctan uca test eder. Her adim gercek veritabani
/// islemleri yapar, stok duser, fatura kesilir, muhasebe kaydi olusur.
///
/// Testcontainers ile Docker'da PostgreSQL 17 ayaga kalkar,
/// EF Core migration'lari uygulanir, tam izole test ortami saglanir.
///
///   1. Tenant olustur ("Ahmet Ticaret")
///   2. Magaza ekle (Trendyol magazasi)
///   3. 5 urun ekle (gercekci Turk urun adlari, fiyat TRY)
///   4. Stok gir (her urune 100 adet)
///   5. Siparis al (2 siparis, farkli urunler)
///   6. Siparis onayla → stok dusmeli
///   7. Kargo olustur (Yurtici Kargo)
///   8. Fatura olustur
///   9. Hakedis kaydi (settlement)
///  10. Banka kaydi (tahsilat)
///  11. Mutabakat (reconciliation)
///  12. Kar/zarar raporu sorgula
///  13. Urun guncelle (fiyat degisikligi)
///  14. Dashboard KPI kontrolu (toplam satis, siparis sayisi, stok durumu)
/// </summary>
[Trait("Category", "E2E")]
[Trait("Requires", "Docker")]
[Trait("Phase", "Dalga14")]
[Collection("E2E-Testcontainers")]
public class AhmetBeyTestcontainersE2ETests : IClassFixture<PostgreSqlContainerFixture>, IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private AppDbContext _context = null!;
    private readonly SettableTenantProvider _tenantProvider = new();

    // Shared state across steps
    private Guid _tenantId;
    private Guid _storeId;
    private Guid _categoryId;
    private Guid _customerId;
    private Guid _bankAccountId;
    private readonly List<Guid> _productIds = new();
    private readonly List<Guid> _orderIds = new();

    public AhmetBeyTestcontainersE2ETests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    public async Task InitializeAsync()
    {
        _context = new AppDbContext(CreateOptions(), _tenantProvider);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up all test data in reverse FK order
        _context.Set<ReconciliationMatch>().RemoveRange(
            _context.Set<ReconciliationMatch>().IgnoreQueryFilters().Where(r => r.TenantId == _tenantId));
        _context.Set<BankTransaction>().RemoveRange(
            _context.Set<BankTransaction>().IgnoreQueryFilters().Where(b => b.TenantId == _tenantId));
        _context.Set<SettlementLine>().RemoveRange(
            _context.Set<SettlementLine>().IgnoreQueryFilters().Where(s => s.TenantId == _tenantId));
        _context.Set<SettlementBatch>().RemoveRange(
            _context.Set<SettlementBatch>().IgnoreQueryFilters().Where(s => s.TenantId == _tenantId));
        _context.Set<ProfitReport>().RemoveRange(
            _context.Set<ProfitReport>().IgnoreQueryFilters().Where(p => p.TenantId == _tenantId));
        _context.Set<CommissionRecord>().RemoveRange(
            _context.Set<CommissionRecord>().IgnoreQueryFilters().Where(c => c.TenantId == _tenantId));
        _context.Set<JournalLine>().RemoveRange(
            _context.Set<JournalLine>().IgnoreQueryFilters().Where(j => j.TenantId == _tenantId));
        _context.Set<JournalEntry>().RemoveRange(
            _context.Set<JournalEntry>().IgnoreQueryFilters().Where(j => j.TenantId == _tenantId));
        _context.Set<ChartOfAccounts>().RemoveRange(
            _context.Set<ChartOfAccounts>().IgnoreQueryFilters().Where(c => c.TenantId == _tenantId));
        _context.Set<InvoiceLine>().RemoveRange(
            _context.Set<InvoiceLine>().IgnoreQueryFilters().Where(il => il.TenantId == _tenantId));
        _context.Invoices.RemoveRange(
            _context.Invoices.IgnoreQueryFilters().Where(i => i.TenantId == _tenantId));
        _context.StockMovements.RemoveRange(
            _context.StockMovements.IgnoreQueryFilters().Where(sm => sm.TenantId == _tenantId));
        _context.OrderItems.RemoveRange(
            _context.OrderItems.IgnoreQueryFilters().Where(oi => oi.TenantId == _tenantId));
        _context.Orders.RemoveRange(
            _context.Orders.IgnoreQueryFilters().Where(o => o.TenantId == _tenantId));
        _context.Products.RemoveRange(
            _context.Products.IgnoreQueryFilters().Where(p => p.TenantId == _tenantId));
        _context.Set<Customer>().RemoveRange(
            _context.Set<Customer>().IgnoreQueryFilters().Where(c => c.TenantId == _tenantId));
        _context.Set<BankAccount>().RemoveRange(
            _context.Set<BankAccount>().IgnoreQueryFilters().Where(b => b.TenantId == _tenantId));
        _context.StoreCredentials.RemoveRange(
            _context.StoreCredentials.IgnoreQueryFilters().Where(sc => sc.TenantId == _tenantId));
        _context.Stores.RemoveRange(
            _context.Stores.IgnoreQueryFilters().Where(s => s.TenantId == _tenantId));
        _context.Categories.RemoveRange(
            _context.Categories.IgnoreQueryFilters().Where(c => c.TenantId == _tenantId));
        _context.Set<Tenant>().RemoveRange(
            _context.Set<Tenant>().IgnoreQueryFilters().Where(t => t.Id == _tenantId));
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14-Step Ahmet Bey Full Business Scenario
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "AhmetBey E2E — 14 adim gercek PostgreSQL ile tam is senaryosu")]
    public async Task AhmetBey_FullBusinessScenario_14Steps_RealPostgres()
    {
        // ─── Step 01: Tenant Olustur ─────────────────────────────────────
        await Step01_CreateTenant();

        // ─── Step 02: Magaza Ekle ────────────────────────────────────────
        await Step02_AddStore();

        // ─── Step 03: 5 Urun Ekle ───────────────────────────────────────
        await Step03_Add5Products();

        // ─── Step 04: Stok Gir ───────────────────────────────────────────
        await Step04_EnterStock();

        // ─── Step 05: Siparis Al ─────────────────────────────────────────
        await Step05_ReceiveOrders();

        // ─── Step 06: Siparis Onayla (stok dusmeli) ──────────────────────
        await Step06_ConfirmOrdersAndDeductStock();

        // ─── Step 07: Kargo Olustur ──────────────────────────────────────
        await Step07_CreateShipment();

        // ─── Step 08: Fatura Olustur ─────────────────────────────────────
        await Step08_CreateInvoice();

        // ─── Step 09: Hakedis Kaydi ──────────────────────────────────────
        await Step09_RecordSettlement();

        // ─── Step 10: Banka Kaydi ────────────────────────────────────────
        await Step10_RecordBankTransaction();

        // ─── Step 11: Mutabakat ──────────────────────────────────────────
        await Step11_RunReconciliation();

        // ─── Step 12: Kar/Zarar Raporu ───────────────────────────────────
        await Step12_GenerateProfitReport();

        // ─── Step 13: Urun Guncelle ─────────────────────────────────────
        await Step13_UpdateProductPrice();

        // ─── Step 14: Dashboard KPI Kontrolu ─────────────────────────────
        await Step14_VerifyDashboardKpis();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step Implementations
    // ══════════════════════════════════════════════════════════════════════════

    private async Task Step01_CreateTenant()
    {
        var tenant = new Tenant
        {
            Name = "Ahmet Ticaret",
            TaxNumber = "1234567890",
            IsActive = true
        };
        _tenantId = tenant.Id;

        _context.Set<Tenant>().Add(tenant);
        await _context.SaveChangesAsync();

        // Switch the tenant provider to our new tenant
        _tenantProvider.SetTenant(_tenantId);

        // Verify persisted
        var saved = await _context.Set<Tenant>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _tenantId);

        saved.Should().NotBeNull("Tenant must be persisted in PostgreSQL");
        saved!.Name.Should().Be("Ahmet Ticaret");
        saved.TaxNumber.Should().Be("1234567890");
        saved.IsActive.Should().BeTrue();
    }

    private async Task Step02_AddStore()
    {
        var store = new Store
        {
            TenantId = _tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Ahmet Ticaret Trendyol",
            ExternalStoreId = "TY-789456",
            IsActive = true
        };
        _storeId = store.Id;

        // Add API credential
        var credential = new StoreCredential
        {
            TenantId = _tenantId,
            StoreId = store.Id,
            Key = "ApiKey",
            EncryptedValue = "enc-test-api-key-ahmet"
        };
        store.Credentials.Add(credential);

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        // Verify
        var saved = await _context.Stores
            .Include(s => s.Credentials)
            .FirstOrDefaultAsync(s => s.Id == _storeId);

        saved.Should().NotBeNull();
        saved!.PlatformType.Should().Be(PlatformType.Trendyol);
        saved.StoreName.Should().Contain("Trendyol");
        saved.Credentials.Should().HaveCount(1);
        saved.Credentials.First().Key.Should().Be("ApiKey");
    }

    private async Task Step03_Add5Products()
    {
        // Create category first
        var category = new Category
        {
            TenantId = _tenantId,
            Name = "Kozmetik",
            Code = "KOZ",
            IsActive = true
        };
        _categoryId = category.Id;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Realistic Turkish cosmetic product data
        var productData = new[]
        {
            ("Nemlendirici Yuz Kremi 50ml", "KOZ-0001", "8690001000001", 45.00m, 129.90m),
            ("Gunes Koruyucu SPF50 100ml", "KOZ-0002", "8690001000002", 62.50m, 189.90m),
            ("Anti-Aging Serum 30ml", "KOZ-0003", "8690001000003", 85.00m, 249.90m),
            ("Dudak Bakim Yagi 15ml", "KOZ-0004", "8690001000004", 18.00m, 59.90m),
            ("Goz Alti Kremi 20ml", "KOZ-0005", "8690001000005", 55.00m, 169.90m)
        };

        var products = productData.Select(d => new Product
        {
            TenantId = _tenantId,
            Name = d.Item1,
            SKU = d.Item2,
            Barcode = d.Item3,
            PurchasePrice = d.Item4,
            SalePrice = d.Item5,
            Stock = 0, // Step 04 will add stock
            MinimumStock = 10,
            CategoryId = _categoryId,
            IsActive = true,
            TaxRate = 0.20m, // Kozmetik KDV %20
            CurrencyCode = "TRY"
        }).ToList();

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        _productIds.AddRange(products.Select(p => p.Id));

        // Verify all 5 products persisted
        var productRepo = new ProductRepository(_context);
        var allProducts = await productRepo.GetByCategoryAsync(_categoryId);

        allProducts.Should().HaveCount(5, "5 kozmetik urun veritabaninda olmali");
        allProducts.Should().OnlyContain(p => p.TenantId == _tenantId);
        allProducts.Select(p => p.SKU).Should().OnlyHaveUniqueItems();
        allProducts.Should().OnlyContain(p => p.SalePrice > p.PurchasePrice,
            "satis fiyati alis fiyatindan yuksek olmali");
    }

    private async Task Step04_EnterStock()
    {
        var products = await _context.Products
            .Where(p => _productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            product.AdjustStock(100, StockMovementType.Purchase, "Ilk stok girisi — tedarikci alimi");

            // Create explicit stock movement record
            var movement = new StockMovement
            {
                TenantId = _tenantId,
                ProductId = product.Id,
                Quantity = 100,
                MovementType = StockMovementType.Purchase.ToString(),
                Reason = "Ilk stok girisi — tedarikci alimi",
                Date = DateTime.UtcNow
            };
            movement.SetStockLevels(0, 100);
            movement.Approve("system");
            _context.StockMovements.Add(movement);
        }
        await _context.SaveChangesAsync();

        // Verify stock levels
        var updatedProducts = await _context.Products
            .Where(p => _productIds.Contains(p.Id))
            .ToListAsync();

        updatedProducts.Should().OnlyContain(p => p.Stock == 100,
            "her urune 100 adet stok girilmis olmali");

        var movements = await _context.StockMovements
            .Where(sm => _productIds.Contains(sm.ProductId))
            .ToListAsync();

        movements.Should().HaveCount(5, "her urun icin 1 stok hareketi olmali");
        movements.Should().OnlyContain(m => m.Quantity == 100);
        movements.Should().OnlyContain(m => m.MovementType == "Purchase");
    }

    private async Task Step05_ReceiveOrders()
    {
        // Create customer
        var customer = new Customer
        {
            TenantId = _tenantId,
            Name = "Fatma Yilmaz",
            Code = "MUS-001",
            Email = "fatma@example.com",
            Phone = "05321234567",
            City = "Istanbul",
            ShippingAddress = "Kadikoy, Bagdat Cad. No:42/3, Istanbul",
            Country = "TR",
            IsActive = true
        };
        _customerId = customer.Id;
        _context.Set<Customer>().Add(customer);
        await _context.SaveChangesAsync();

        var products = await _context.Products
            .Where(p => _productIds.Contains(p.Id))
            .OrderBy(p => p.SKU)
            .ToListAsync();

        // Order 1: Nemlendirici (x2) + Gunes Koruyucu (x1)
        var order1 = new Order
        {
            TenantId = _tenantId,
            OrderNumber = "TY-2026-50001",
            CustomerId = _customerId,
            CustomerName = "Fatma Yilmaz",
            CustomerEmail = "fatma@example.com",
            Status = OrderStatus.Pending,
            SourcePlatform = PlatformType.Trendyol,
            ExternalOrderId = "TY-EXT-50001",
            OrderDate = DateTime.UtcNow
        };

        var item1a = new OrderItem
        {
            TenantId = _tenantId,
            OrderId = order1.Id,
            ProductId = products[0].Id,
            ProductName = products[0].Name,
            ProductSKU = products[0].SKU,
            Quantity = 2,
            UnitPrice = products[0].SalePrice,
            TaxRate = 0.20m
        };
        item1a.CalculateAmounts();
        order1.AddItem(item1a);

        var item1b = new OrderItem
        {
            TenantId = _tenantId,
            OrderId = order1.Id,
            ProductId = products[1].Id,
            ProductName = products[1].Name,
            ProductSKU = products[1].SKU,
            Quantity = 1,
            UnitPrice = products[1].SalePrice,
            TaxRate = 0.20m
        };
        item1b.CalculateAmounts();
        order1.AddItem(item1b);

        // Order 2: Anti-Aging Serum (x1) + Dudak Bakim (x3) + Goz Alti (x1)
        var order2 = new Order
        {
            TenantId = _tenantId,
            OrderNumber = "TY-2026-50002",
            CustomerId = _customerId,
            CustomerName = "Fatma Yilmaz",
            CustomerEmail = "fatma@example.com",
            Status = OrderStatus.Pending,
            SourcePlatform = PlatformType.Trendyol,
            ExternalOrderId = "TY-EXT-50002",
            OrderDate = DateTime.UtcNow
        };

        var item2a = new OrderItem
        {
            TenantId = _tenantId,
            OrderId = order2.Id,
            ProductId = products[2].Id,
            ProductName = products[2].Name,
            ProductSKU = products[2].SKU,
            Quantity = 1,
            UnitPrice = products[2].SalePrice,
            TaxRate = 0.20m
        };
        item2a.CalculateAmounts();
        order2.AddItem(item2a);

        var item2b = new OrderItem
        {
            TenantId = _tenantId,
            OrderId = order2.Id,
            ProductId = products[3].Id,
            ProductName = products[3].Name,
            ProductSKU = products[3].SKU,
            Quantity = 3,
            UnitPrice = products[3].SalePrice,
            TaxRate = 0.20m
        };
        item2b.CalculateAmounts();
        order2.AddItem(item2b);

        var item2c = new OrderItem
        {
            TenantId = _tenantId,
            OrderId = order2.Id,
            ProductId = products[4].Id,
            ProductName = products[4].Name,
            ProductSKU = products[4].SKU,
            Quantity = 1,
            UnitPrice = products[4].SalePrice,
            TaxRate = 0.20m
        };
        item2c.CalculateAmounts();
        order2.AddItem(item2c);

        _context.Orders.Add(order1);
        _context.Orders.Add(order2);
        await _context.SaveChangesAsync();

        _orderIds.Add(order1.Id);
        _orderIds.Add(order2.Id);

        // Verify orders
        var savedOrders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => _orderIds.Contains(o.Id))
            .ToListAsync();

        savedOrders.Should().HaveCount(2);
        savedOrders.Should().OnlyContain(o => o.Status == OrderStatus.Pending);
        savedOrders.Should().OnlyContain(o => o.SourcePlatform == PlatformType.Trendyol);
        savedOrders.Should().OnlyContain(o => o.TotalAmount > 0,
            "siparis toplami pozitif olmali");

        var totalItemCount = savedOrders.Sum(o => o.OrderItems.Count);
        totalItemCount.Should().Be(5, "toplam 5 siparis kalemi olmali (2+3)");
    }

    private async Task Step06_ConfirmOrdersAndDeductStock()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => _orderIds.Contains(o.Id))
            .ToListAsync();

        foreach (var order in orders)
        {
            // Confirm order
            order.Place();

            // Deduct stock for each item
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                product.Should().NotBeNull();

                var previousStock = product!.Stock;
                product.AdjustStock(-item.Quantity, StockMovementType.Sale,
                    $"Siparis {order.OrderNumber} — stok dusumu");

                // Record stock movement
                var movement = new StockMovement
                {
                    TenantId = _tenantId,
                    ProductId = product.Id,
                    OrderId = order.Id,
                    Quantity = -item.Quantity,
                    MovementType = StockMovementType.Sale.ToString(),
                    Reason = $"Siparis {order.OrderNumber}",
                    Date = DateTime.UtcNow
                };
                movement.SetStockLevels(previousStock, product.Stock);
                movement.Approve("system");
                _context.StockMovements.Add(movement);
            }
        }
        await _context.SaveChangesAsync();

        // Verify order statuses
        var confirmedOrders = await _context.Orders
            .Where(o => _orderIds.Contains(o.Id))
            .ToListAsync();
        confirmedOrders.Should().OnlyContain(o => o.Status == OrderStatus.Confirmed);

        // Verify stock deductions
        // Product 0 (Nemlendirici): 100 - 2 = 98
        // Product 1 (Gunes): 100 - 1 = 99
        // Product 2 (Anti-Aging): 100 - 1 = 99
        // Product 3 (Dudak Bakim): 100 - 3 = 97
        // Product 4 (Goz Alti): 100 - 1 = 99
        var products = await _context.Products
            .Where(p => _productIds.Contains(p.Id))
            .OrderBy(p => p.SKU)
            .ToListAsync();

        products[0].Stock.Should().Be(98, "Nemlendirici: 100 - 2 = 98");
        products[1].Stock.Should().Be(99, "Gunes Koruyucu: 100 - 1 = 99");
        products[2].Stock.Should().Be(99, "Anti-Aging: 100 - 1 = 99");
        products[3].Stock.Should().Be(97, "Dudak Bakim: 100 - 3 = 97");
        products[4].Stock.Should().Be(99, "Goz Alti: 100 - 1 = 99");

        // Total stock movements should be 10 (5 purchase + 5 sale items)
        var allMovements = await _context.StockMovements
            .Where(sm => _productIds.Contains(sm.ProductId))
            .ToListAsync();
        allMovements.Should().HaveCount(10);
    }

    private async Task Step07_CreateShipment()
    {
        // Ship first order via Yurtici Kargo
        var order1 = await _context.Orders.FindAsync(_orderIds[0]);
        order1.Should().NotBeNull();
        order1!.Status.Should().Be(OrderStatus.Confirmed,
            "siparis kargoya verilmeden once Confirmed olmali");

        order1.MarkAsShipped("YK-AHMET-2026-001", CargoProvider.YurticiKargo);
        await _context.SaveChangesAsync();

        // Verify
        var shipped = await _context.Orders.FindAsync(_orderIds[0]);
        shipped!.Status.Should().Be(OrderStatus.Shipped);
        shipped.TrackingNumber.Should().Be("YK-AHMET-2026-001");
        shipped.CargoProvider.Should().Be(CargoProvider.YurticiKargo);
        shipped.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    private async Task Step08_CreateInvoice()
    {
        // Create invoice for the shipped order
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.Id == _orderIds[0]);

        var invoice = MesTech.Domain.Entities.Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-2026-AHM-001");
        invoice.StoreId = _storeId;

        // Add invoice lines for each order item
        foreach (var item in order.OrderItems)
        {
            var line = new InvoiceLine
            {
                TenantId = _tenantId,
                InvoiceId = invoice.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                SKU = item.ProductSKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount
            };
            line.CalculateLineTotal();
            invoice.AddLine(line);
        }

        // Simulate GIB e-fatura gonderimi
        var gibId = "GIB-AHM-2026-00001";
        var pdfUrl = $"https://efatura.mestech.com/{gibId}.pdf";
        invoice.MarkAsSent(gibId, pdfUrl);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Verify invoice
        var saved = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.OrderId == _orderIds[0]);

        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-2026-AHM-001");
        saved.Type.Should().Be(InvoiceType.EFatura);
        saved.Status.Should().Be(InvoiceStatus.Sent);
        saved.GibInvoiceId.Should().Be("GIB-AHM-2026-00001");
        saved.PdfUrl.Should().Contain(".pdf");
        saved.GrandTotal.Should().BeGreaterThan(0);
        saved.Lines.Should().HaveCount(2, "siparis 2 kalemli, fatura da 2 satirli olmali");
        saved.CustomerName.Should().Be("Fatma Yilmaz");
    }

    private async Task Step09_RecordSettlement()
    {
        // Simulate Trendyol settlement (hakedis) for both orders
        var orders = await _context.Orders
            .Where(o => _orderIds.Contains(o.Id))
            .ToListAsync();

        var totalGross = orders.Sum(o => o.TotalAmount);
        var commissionRate = 0.15m; // Trendyol kozmetik komisyonu %15
        var totalCommission = totalGross * commissionRate;
        var totalNet = totalGross - totalCommission;

        var settlement = SettlementBatch.Create(
            _tenantId,
            "Trendyol",
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date,
            totalGross,
            totalCommission,
            totalNet);

        _context.Set<SettlementBatch>().Add(settlement);

        // Add settlement lines for each order
        foreach (var order in orders)
        {
            var orderCommission = order.TotalAmount * commissionRate;
            var cargoDeduction = 29.90m; // Trendyol kargo kesintisi
            var netAmount = order.TotalAmount - orderCommission - cargoDeduction;

            var line = SettlementLine.Create(
                _tenantId,
                settlement.Id,
                order.OrderNumber,
                order.TotalAmount,
                orderCommission,
                serviceFee: 0m,
                cargoDeduction: cargoDeduction,
                refundDeduction: 0m,
                netAmount: netAmount);

            settlement.AddLine(line);
            _context.Set<SettlementLine>().Add(line);

            // Create commission record
            var commissionRecord = CommissionRecord.Create(
                _tenantId,
                "Trendyol",
                order.TotalAmount,
                commissionRate,
                orderCommission,
                serviceFee: 0m,
                orderId: order.OrderNumber,
                category: "Kozmetik",
                rateSource: "Trendyol API");

            _context.Set<CommissionRecord>().Add(commissionRecord);

            // Update order with commission info
            order.SetCommission(commissionRate, orderCommission);
            order.SetCargoExpense(cargoDeduction);
        }
        await _context.SaveChangesAsync();

        // Verify
        var savedBatch = await _context.Set<SettlementBatch>()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.TenantId == _tenantId);

        savedBatch.Should().NotBeNull();
        savedBatch!.Platform.Should().Be("Trendyol");
        savedBatch.TotalGross.Should().Be(totalGross);
        savedBatch.TotalCommission.Should().Be(totalCommission);
        savedBatch.TotalNet.Should().Be(totalNet);
        savedBatch.Lines.Should().HaveCount(2);
        savedBatch.Lines.Should().OnlyContain(l => l.CommissionAmount > 0);
        savedBatch.Lines.Should().OnlyContain(l => l.CargoDeduction == 29.90m);

        var commissions = await _context.Set<CommissionRecord>()
            .Where(c => c.TenantId == _tenantId)
            .ToListAsync();
        commissions.Should().HaveCount(2);
        commissions.Should().OnlyContain(c => c.CommissionRate == commissionRate);
    }

    private async Task Step10_RecordBankTransaction()
    {
        // Create bank account for Ahmet Ticaret
        var bankAccount = BankAccount.Create(
            _tenantId,
            "Ahmet Ticaret Isbank",
            bankName: "Turkiye Is Bankasi",
            iban: "TR330006100519786457841326",
            isDefault: true);
        _bankAccountId = bankAccount.Id;

        _context.Set<BankAccount>().Add(bankAccount);
        await _context.SaveChangesAsync();

        // Record Trendyol settlement payment as bank transaction
        var settlement = await _context.Set<SettlementBatch>()
            .FirstAsync(s => s.TenantId == _tenantId);

        var bankTx = BankTransaction.Create(
            _tenantId,
            _bankAccountId,
            DateTime.UtcNow,
            settlement.TotalNet,
            "TRENDYOL HAKEDIS ODEMESI",
            referenceNumber: $"TY-HAK-{settlement.Id.ToString()[..8].ToUpperInvariant()}");

        _context.Set<BankTransaction>().Add(bankTx);
        await _context.SaveChangesAsync();

        // Verify
        var savedTx = await _context.Set<BankTransaction>()
            .FirstOrDefaultAsync(t => t.TenantId == _tenantId);

        savedTx.Should().NotBeNull();
        savedTx!.Amount.Should().Be(settlement.TotalNet);
        savedTx.Description.Should().Contain("TRENDYOL");
        savedTx.IsReconciled.Should().BeFalse("henuz mutabakat yapilmadi");
        savedTx.BankAccountId.Should().Be(_bankAccountId);
    }

    private async Task Step11_RunReconciliation()
    {
        var settlement = await _context.Set<SettlementBatch>()
            .FirstAsync(s => s.TenantId == _tenantId);

        var bankTx = await _context.Set<BankTransaction>()
            .FirstAsync(t => t.TenantId == _tenantId);

        // Amounts match — auto-reconcile with high confidence
        var amountDiff = Math.Abs(settlement.TotalNet - bankTx.Amount);
        var confidence = amountDiff == 0 ? 1.0m : Math.Max(0, 1.0m - (amountDiff / settlement.TotalNet));

        var match = ReconciliationMatch.Create(
            _tenantId,
            DateTime.UtcNow,
            confidence,
            ReconciliationStatus.AutoMatched,
            settlementBatchId: settlement.Id,
            bankTransactionId: bankTx.Id);

        _context.Set<ReconciliationMatch>().Add(match);

        // Mark both as reconciled
        settlement.MarkReconciled();
        bankTx.MarkReconciled();

        await _context.SaveChangesAsync();

        // Verify
        var savedMatch = await _context.Set<ReconciliationMatch>()
            .FirstOrDefaultAsync(r => r.TenantId == _tenantId);

        savedMatch.Should().NotBeNull();
        savedMatch!.Confidence.Should().Be(1.0m, "tutarlar birebir eslestiginde guven %100 olmali");
        savedMatch.Status.Should().Be(ReconciliationStatus.AutoMatched);
        savedMatch.SettlementBatchId.Should().Be(settlement.Id);
        savedMatch.BankTransactionId.Should().Be(bankTx.Id);

        // Verify settlement status
        var updatedSettlement = await _context.Set<SettlementBatch>()
            .FirstAsync(s => s.Id == settlement.Id);
        updatedSettlement.Status.Should().Be(SettlementStatus.Reconciled);

        // Verify bank transaction status
        var updatedTx = await _context.Set<BankTransaction>()
            .FirstAsync(t => t.Id == bankTx.Id);
        updatedTx.IsReconciled.Should().BeTrue();
    }

    private async Task Step12_GenerateProfitReport()
    {
        // Gather data from DB
        var orders = await _context.Orders
            .Where(o => _orderIds.Contains(o.Id))
            .ToListAsync();

        var products = await _context.Products
            .Where(p => _productIds.Contains(p.Id))
            .ToListAsync();

        var orderItems = await _context.OrderItems
            .Where(oi => _orderIds.Contains(oi.OrderId))
            .ToListAsync();

        // Calculate COGS (cost of goods sold)
        decimal totalCogs = 0;
        foreach (var item in orderItems)
        {
            var product = products.First(p => p.Id == item.ProductId);
            totalCogs += product.PurchasePrice * item.Quantity;
        }

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalCommission = orders.Sum(o => o.CommissionAmount ?? 0);
        var totalCargo = orders.Sum(o => o.CargoExpenseAmount ?? 0);
        var totalTax = orders.Sum(o => o.TaxAmount);

        var profitReport = ProfitReport.Create(
            _tenantId,
            DateTime.UtcNow,
            "2026-03-W11",
            totalRevenue,
            totalCogs,
            totalCommission,
            totalCargo,
            totalTax,
            platform: "Trendyol");

        _context.Set<ProfitReport>().Add(profitReport);

        // Also create journal entries for the accounting record
        var accountReceivable = ChartOfAccounts.Create(
            _tenantId, "120.01", "Alicilar - Trendyol", AccountingAccountType.Asset, level: 2);
        var salesRevenue = ChartOfAccounts.Create(
            _tenantId, "600.01", "Yurtici Satislar", AccountingAccountType.Revenue, level: 2);
        var vatPayable = ChartOfAccounts.Create(
            _tenantId, "391.01", "Hesaplanan KDV", AccountingAccountType.Liability, level: 2);

        _context.Set<ChartOfAccounts>().AddRange(accountReceivable, salesRevenue, vatPayable);
        await _context.SaveChangesAsync();

        var journalEntry = JournalEntry.Create(
            _tenantId,
            DateTime.UtcNow,
            "Trendyol haftalik satis kaydi — Ahmet Ticaret",
            referenceNumber: "JE-2026-AHM-001");

        journalEntry.AddLine(accountReceivable.Id, debit: totalRevenue, credit: 0, "Alicilar — Trendyol");
        var subTotal = totalRevenue - totalTax;
        journalEntry.AddLine(salesRevenue.Id, debit: 0, credit: subTotal, "Yurtici Satislar");
        journalEntry.AddLine(vatPayable.Id, debit: 0, credit: totalTax, "Hesaplanan KDV %20");
        journalEntry.Validate();
        journalEntry.Post();

        _context.Set<JournalEntry>().Add(journalEntry);
        await _context.SaveChangesAsync();

        // Verify profit report
        var savedReport = await _context.Set<ProfitReport>()
            .FirstOrDefaultAsync(r => r.TenantId == _tenantId);

        savedReport.Should().NotBeNull();
        savedReport!.TotalRevenue.Should().Be(totalRevenue);
        savedReport.TotalCost.Should().Be(totalCogs);
        savedReport.TotalCommission.Should().Be(totalCommission);
        savedReport.NetProfit.Should().BeGreaterThan(0, "Ahmet Bey karli olmali");
        savedReport.Platform.Should().Be("Trendyol");
        savedReport.Period.Should().Be("2026-03-W11");

        // Verify journal entry
        var savedEntry = await _context.Set<JournalEntry>()
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.TenantId == _tenantId);

        savedEntry.Should().NotBeNull();
        savedEntry!.IsPosted.Should().BeTrue();
        savedEntry.Lines.Should().HaveCount(3);
        savedEntry.Lines.Sum(l => l.Debit).Should().Be(savedEntry.Lines.Sum(l => l.Credit),
            "cift tarafli kayit: borc = alacak");
    }

    private async Task Step13_UpdateProductPrice()
    {
        // Ahmet Bey decides to increase the Anti-Aging Serum price
        var product = await _context.Products.FindAsync(_productIds[2]);
        product.Should().NotBeNull();

        var oldPrice = product!.SalePrice;
        oldPrice.Should().Be(249.90m);

        var newPrice = 299.90m;
        product.UpdatePrice(newPrice);
        await _context.SaveChangesAsync();

        // Verify
        var updated = await _context.Products.FindAsync(_productIds[2]);
        updated!.SalePrice.Should().Be(299.90m);
        updated.SalePrice.Should().BeGreaterThan(oldPrice,
            "fiyat artisi uygulandi");
        updated.ProfitMargin.Should().BeGreaterThan(0,
            "kar marji hala pozitif olmali");
    }

    private async Task Step14_VerifyDashboardKpis()
    {
        // KPI 1: Total sales (toplam satis)
        var totalSales = await _context.Orders
            .Where(o => _orderIds.Contains(o.Id))
            .SumAsync(o => o.TotalAmount);
        totalSales.Should().BeGreaterThan(0, "toplam satis sifirdan buyuk olmali");

        // KPI 2: Order count (siparis sayisi)
        var orderCount = await _context.Orders
            .Where(o => _orderIds.Contains(o.Id))
            .CountAsync();
        orderCount.Should().Be(2, "2 siparis girildi");

        // KPI 3: Product count (urun sayisi)
        var productCount = await _context.Products
            .Where(p => p.TenantId == _tenantId && p.IsActive)
            .CountAsync();
        productCount.Should().Be(5, "5 aktif urun olmali");

        // KPI 4: Low stock alert count (dusuk stoklu urunler)
        var lowStockCount = await _context.Products
            .Where(p => p.TenantId == _tenantId && p.Stock <= p.MinimumStock)
            .CountAsync();
        lowStockCount.Should().Be(0,
            "100 adet girildi, en fazla 3 adet satildi — hicbir urun dusuk stokta olmamali");

        // KPI 5: Total stock value (toplam stok degeri)
        var products = await _context.Products
            .Where(p => p.TenantId == _tenantId)
            .ToListAsync();
        var totalStockValue = products.Sum(p => p.Stock * p.PurchasePrice);
        totalStockValue.Should().BeGreaterThan(0, "stok degeri pozitif olmali");

        // KPI 6: Total stock units
        var totalStockUnits = products.Sum(p => p.Stock);
        totalStockUnits.Should().Be(492,
            "500 adet girildi, toplam 8 adet satildi (2+1+1+3+1) = 492 kalmali");

        // KPI 7: Invoiced orders (fatura kesilen siparisler)
        var invoicedCount = await _context.Invoices
            .Where(i => i.TenantId == _tenantId && i.Status == InvoiceStatus.Sent)
            .CountAsync();
        invoicedCount.Should().BeGreaterOrEqualTo(1,
            "en az 1 siparis faturalandi");

        // KPI 8: Settlement reconciliation status
        var reconciledCount = await _context.Set<ReconciliationMatch>()
            .Where(r => r.TenantId == _tenantId && r.Status == ReconciliationStatus.AutoMatched)
            .CountAsync();
        reconciledCount.Should().BeGreaterOrEqualTo(1,
            "en az 1 mutabakat eslesmesi yapildi");

        // KPI 9: Profit margin check
        var profitReport = await _context.Set<ProfitReport>()
            .FirstAsync(r => r.TenantId == _tenantId);
        profitReport.NetProfit.Should().BeGreaterThan(0, "isletme karli olmali");

        // KPI 10: Average order value
        var avgOrderValue = totalSales / orderCount;
        avgOrderValue.Should().BeGreaterThan(100m,
            "kozmetik siparislerinin ortalamasi 100 TL uzeri olmali");

        // KPI 11: Stock movement history
        var movementCount = await _context.StockMovements
            .Where(sm => _productIds.Contains(sm.ProductId))
            .CountAsync();
        movementCount.Should().Be(10,
            "5 alis + 5 satis = 10 stok hareketi olmali");

        // KPI 12: Verify no data leaks (multi-tenant isolation)
        var fakeProvider = new SettableTenantProvider();
        fakeProvider.SetTenant(Guid.NewGuid()); // random tenant
        await using var isolatedContext = new AppDbContext(CreateOptions(), fakeProvider);

        var leakedProducts = await isolatedContext.Products.ToListAsync();
        leakedProducts.Should().BeEmpty(
            "farkli tenant'in urunleri gorulmemeli — multi-tenant izolasyonu");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper: Settable Tenant Provider
    // ══════════════════════════════════════════════════════════════════════════

    private sealed class SettableTenantProvider : ITenantProvider
    {
        private Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Guid GetCurrentTenantId() => _tenantId;
        public void SetTenant(Guid tenantId) => _tenantId = tenantId;
    }
}
