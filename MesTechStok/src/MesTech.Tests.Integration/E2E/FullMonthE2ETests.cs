using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using DomainInvoice = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// TAM AY İŞ DÖNGÜSÜ E2E TESTİ — EMR-20260319-T008
///
/// Senaryo: Bir MesTech satıcısının 1 aylık iş döngüsünü simüle eder.
///
/// Akış:
///   1. Ürün oluştur (5 ürün, 3 kategori)
///   2. Stok güncelle (her ürüne 100 adet)
///   3. Sipariş al (10 sipariş, farklı platformlar)
///   4. Fatura kes (10 fatura, KDV hesaplamalı)
///   5. Kargo gönder (Order üzerinde CargoProvider + TrackingNumber)
///   6. Mutabakat (platform ödemeleri ile eşleştir)
///   7. Ay sonu rapor (stok, KDV, kâr/zarar)
///
/// Docker gerektirir. Docker yoksa otomatik skip olur.
/// Testcontainers ile gerçek PostgreSQL 17 container başlatır.
///
/// FUTURE — MediatR entegrasyonu:
///   Scaffold direkt DB yazıyor. CQRS handler'lar bağlandığında
///   CreateProductCommand, AddStockCommand, PlaceOrderCommand,
///   CreateEInvoiceCommand, AutoShipOrderCommand, RunReconciliationCommand,
///   GetTrialBalanceQuery, GetBalanceSheetQuery, GetFifoCOGSQuery kullanılacak.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Requires", "Docker")]
[Trait("Phase", "Dalga15")]
[Collection("E2E-Testcontainers")]
[TestCaseOrderer(
    "MesTech.Tests.Integration.E2E.PriorityOrderer",
    "MesTech.Tests.Integration")]
public class FullMonthE2ETests : IClassFixture<PostgreSqlContainerFixture>, IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private AppDbContext _context = null!;
    private readonly SettableTenantProvider _tenantProvider = new();

    // ══════════════════════════════════════════════════════════════════
    // Shared state — testler sıralı çalışır (PriorityOrderer)
    // ══════════════════════════════════════════════════════════════════
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static Guid _categoryElektronikId;
    private static Guid _categoryGiyimId;
    private static Guid _categoryEvYasamId;
    private static Guid _customerId;

    private static readonly List<Guid> _productIds = new();
    private static readonly List<decimal> _salePrices = new();
    private static readonly List<decimal> _purchasePrices = new();
    private static readonly List<Guid> _orderIds = new();
    private static readonly List<Guid> _invoiceIds = new();

    // Test verileri
    private static readonly (string Name, string Sku, string Barcode, decimal Purchase, decimal Sale, string Cat)[] TestProducts =
    {
        ("Samsung Galaxy A55 Kılıf", "SKU-E2E-001", "8680001000010", 25.00m, 149.90m, "Elektronik"),
        ("USB-C Hızlı Şarj Kablosu", "SKU-E2E-002", "8680001000027", 15.00m, 89.90m, "Elektronik"),
        ("Oversize Kadın T-Shirt Beyaz", "SKU-E2E-003", "8680001000034", 45.00m, 259.90m, "Giyim"),
        ("Erkek Spor Çorap 6'lı Paket", "SKU-E2E-004", "8680001000041", 12.00m, 39.90m, "Giyim"),
        ("Bambu Mutfak Düzenleyici Set", "SKU-E2E-005", "8680001000058", 80.00m, 499.90m, "Ev & Yaşam"),
    };

    private static readonly (string Platform, int ProductIndex, int Qty, string ExtOrderId)[] TestOrders =
    {
        ("Trendyol",     0, 2, "TY-240301-1001"),
        ("Trendyol",     1, 1, "TY-240301-1002"),
        ("Hepsiburada",  2, 3, "HB-240303-2001"),
        ("Hepsiburada",  3, 1, "HB-240305-2002"),
        ("N11",          4, 2, "N11-240307-3001"),
        ("N11",          0, 1, "N11-240310-3002"),
        ("Ciceksepeti",  1, 2, "CS-240312-4001"),
        ("Amazon",       2, 1, "AMZ-240315-5001"),
        ("Pazarama",     3, 4, "PZ-240318-6001"),
        ("Trendyol",     4, 1, "TY-240320-1003"),
    };

    // Toplam satılan: 2+1+3+1+2+1+2+1+4+1 = 18 adet
    private const int TotalSoldQty = 18;
    private const int InitialStockPerProduct = 100;
    private const int TotalInitialStock = 500; // 5 × 100
    private const int ExpectedRemainingStock = 482; // 500 - 18

    public FullMonthE2ETests(PostgreSqlContainerFixture fixture)
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
        DockerHelper.SkipIfNoDocker();

        _tenantProvider.SetTenant(_tenantId);
        _context = new AppDbContext(CreateOptions(), _tenantProvider);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 1: ÜRÜN OLUŞTURMA
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(1)]
    public async Task Step01_CreateProducts_ShouldCreate5Products()
    {
        DockerHelper.SkipIfNoDocker();

        // Arrange — Kategoriler oluştur
        var catElektronik = new Category { Name = "Elektronik", TenantId = _tenantId };
        var catGiyim = new Category { Name = "Giyim", TenantId = _tenantId };
        var catEvYasam = new Category { Name = "Ev & Yaşam", TenantId = _tenantId };
        _context.Categories.AddRange(catElektronik, catGiyim, catEvYasam);
        await _context.SaveChangesAsync();

        _categoryElektronikId = catElektronik.Id;
        _categoryGiyimId = catGiyim.Id;
        _categoryEvYasamId = catEvYasam.Id;

        // Act — 5 ürün oluştur
        foreach (var (name, sku, barcode, purchase, sale, cat) in TestProducts)
        {
            var categoryId = cat switch
            {
                "Elektronik" => _categoryElektronikId,
                "Giyim" => _categoryGiyimId,
                "Ev & Yaşam" => _categoryEvYasamId,
                _ => _categoryElektronikId
            };

            var product = new Product
            {

                Name = name,
                SKU = sku,
                Barcode = barcode,
                PurchasePrice = purchase,
                SalePrice = sale,
                CategoryId = categoryId,
                TenantId = _tenantId,
                MinimumStock = 5,
                MaximumStock = 1000,
                TaxRate = 0.20m,
                IsActive = true
            };
            _context.Products.Add(product);
            _productIds.Add(product.Id);
            _salePrices.Add(sale);
            _purchasePrices.Add(purchase);
        }
        await _context.SaveChangesAsync();

        // Assert
        _productIds.Should().HaveCount(5, "5 ürün oluşturulmalı");

        var dbProducts = await _context.Products
            .Where(p => p.TenantId == _tenantId)
            .ToListAsync();
        dbProducts.Should().HaveCount(5);
        dbProducts.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.SKU.Should().StartWith("SKU-E2E-");
            p.TaxRate.Should().Be(0.20m, "KDV oranı %20 olmalı");
        });
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 2: STOK GÜNCELLEME
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(2)]
    public async Task Step02_UpdateStock_ShouldSet100UnitsEach()
    {
        DockerHelper.SkipIfNoDocker();

        // Arrange — her ürüne 100 adet stok ekle
        foreach (var productId in _productIds)
        {
            var product = await _context.Products.FindAsync(productId);

            var movement = new StockMovement
            {

                ProductId = productId,
                Quantity = InitialStockPerProduct,
                UnitCost = product!.PurchasePrice,
                Reason = "İlk stok girişi — E2E test",
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            movement.SetMovementType(StockMovementType.In);
            _context.StockMovements.Add(movement);

            // Ürün stok seviyesini güncelle
            product.Stock = InitialStockPerProduct;
        }
        await _context.SaveChangesAsync();

        // Assert — toplam stok
        var stockSum = await _context.StockMovements
            .Where(s => s.TenantId == _tenantId)
            .SumAsync(s => s.Quantity);
        stockSum.Should().Be(TotalInitialStock, "Toplam giriş stoku 500 olmalı (5×100)");

        // Her ürün 100 adet stokta
        foreach (var productId in _productIds)
        {
            var product = await _context.Products.FindAsync(productId);
            product!.Stock.Should().Be(InitialStockPerProduct);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 3: SİPARİŞ ALMA
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(3)]
    public async Task Step03_ReceiveOrders_ShouldCreate10Orders()
    {
        DockerHelper.SkipIfNoDocker();

        // Arrange — Müşteri oluştur
        var customer = new Customer
        {
            Name = "E2E Test Müşterisi",
            Email = "e2e@mestech.test",
            TenantId = _tenantId
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        _customerId = customer.Id;

        // Act — 10 sipariş oluştur (farklı platformlardan)
        foreach (var (platform, productIndex, qty, extOrderId) in TestOrders)
        {
            var productId = _productIds[productIndex];
            var salePrice = _salePrices[productIndex];

            var platformType = platform switch
            {
                "Trendyol" => PlatformType.Trendyol,
                "Hepsiburada" => PlatformType.Hepsiburada,
                "N11" => PlatformType.N11,
                "Ciceksepeti" => PlatformType.Ciceksepeti,
                "Amazon" => PlatformType.Amazon,
                "Pazarama" => PlatformType.Pazarama,
                _ => PlatformType.Trendyol
            };

            var order = new Order
            {

                CustomerId = _customerId,
                CustomerName = $"{platform} Müşterisi",
                SourcePlatform = platformType,
                ExternalOrderId = extOrderId,
                Status = OrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow.AddDays(-20 + _orderIds.Count),
                SubTotal = salePrice * qty,
                TenantId = _tenantId
            };
            _context.Orders.Add(order);
            _orderIds.Add(order.Id);

            // Stok düş
            var product = await _context.Products.FindAsync(productId);
            product!.Stock -= qty;

            var outMovement = new StockMovement
            {

                ProductId = productId,
                Quantity = -qty,
                UnitCost = product.PurchasePrice,
                Reason = $"Sipariş: {extOrderId}",
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            outMovement.SetMovementType(StockMovementType.Out);
            _context.StockMovements.Add(outMovement);
        }
        await _context.SaveChangesAsync();

        // Assert
        _orderIds.Should().HaveCount(10, "10 sipariş oluşturulmalı");

        var dbOrders = await _context.Orders
            .Where(o => o.TenantId == _tenantId)
            .ToListAsync();
        dbOrders.Should().HaveCount(10);
        dbOrders.Should().AllSatisfy(o =>
        {
            o.Status.Should().Be(OrderStatus.Confirmed);
            o.SubTotal.Should().BeGreaterThan(0);
        });

        // Stok düşmüş olmalı
        var remainingStock = await _context.Products
            .Where(p => p.TenantId == _tenantId)
            .SumAsync(p => p.Stock);
        remainingStock.Should().Be(ExpectedRemainingStock,
            $"Başlangıç {TotalInitialStock} - satılan {TotalSoldQty} = {ExpectedRemainingStock}");
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 4: FATURA KESME
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(4)]
    public async Task Step04_GenerateInvoices_ShouldCreate10Invoices()
    {
        DockerHelper.SkipIfNoDocker();

        // Act — her sipariş için fatura kes
        foreach (var orderId in _orderIds)
        {
            var order = await _context.Orders.FindAsync(orderId);

            var taxPercent = 20;
            var subTotal = order!.SubTotal;
            var taxTotal = subTotal * taxPercent / 100m;
            var grandTotal = subTotal + taxTotal;

            var invoice = new DomainInvoice
            {
                OrderId = orderId,
                InvoiceNumber = $"FAT-E2E-{_invoiceIds.Count + 1:D4}",
                InvoiceDate = DateTime.UtcNow,
                SubTotal = subTotal,
                TaxTotal = taxTotal,
                GrandTotal = grandTotal,
                Currency = "TRY",
                TenantId = _tenantId
            };
            _context.Invoices.Add(invoice);
            _invoiceIds.Add(invoice.Id);
        }
        await _context.SaveChangesAsync();

        // Assert
        _invoiceIds.Should().HaveCount(10, "10 fatura oluşturulmalı");

        var dbInvoices = await _context.Invoices
            .Where(i => i.TenantId == _tenantId)
            .ToListAsync();
        dbInvoices.Should().HaveCount(10);
        dbInvoices.Should().AllSatisfy(inv =>
        {
            inv.TaxTotal.Should().BeGreaterThan(0, "KDV hesaplanmalı");
            inv.GrandTotal.Should().Be(inv.SubTotal + inv.TaxTotal,
                "GrandTotal = SubTotal + TaxTotal");
        });
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 5: KARGO GÖNDERME
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(5)]
    public async Task Step05_ShipOrders_ShouldUpdateCargoInfo()
    {
        DockerHelper.SkipIfNoDocker();

        // Order entity üzerinde CargoProvider + TrackingNumber kullanılır
        // (Ayrı Shipment entity yok — kargo bilgisi Order'a bağlı)
        var providers = new[]
        {
            CargoProvider.YurticiKargo, CargoProvider.ArasKargo, CargoProvider.SuratKargo,
            CargoProvider.YurticiKargo, CargoProvider.ArasKargo, CargoProvider.SuratKargo,
            CargoProvider.YurticiKargo, CargoProvider.ArasKargo, CargoProvider.SuratKargo,
            CargoProvider.YurticiKargo
        };

        // Act — her siparişe kargo bilgisi ekle
        for (int i = 0; i < _orderIds.Count; i++)
        {
            var order = await _context.Orders.FindAsync(_orderIds[i]);
            order!.CargoProvider = providers[i];
            order.TrackingNumber = $"{providers[i]}-{DateTime.UtcNow:yyyyMMdd}-{i + 1:D4}";
            order.Status = OrderStatus.Shipped;
        }
        await _context.SaveChangesAsync();

        // Assert
        var dbOrders = await _context.Orders
            .Where(o => o.TenantId == _tenantId)
            .ToListAsync();
        dbOrders.Should().HaveCount(10);
        dbOrders.Should().AllSatisfy(o =>
        {
            o.TrackingNumber.Should().NotBeNullOrEmpty("Takip no zorunlu");
            o.Status.Should().Be(OrderStatus.Shipped);
            o.CargoProvider.Should().NotBeNull();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 6: MUTABAKAT
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(6)]
    public async Task Step06_Reconciliation_ShouldMatchPayments()
    {
        DockerHelper.SkipIfNoDocker();

        // Arrange — Platform ödeme bildirimleri (SettlementBatch) oluştur
        foreach (var orderId in _orderIds)
        {
            var order = await _context.Orders.FindAsync(orderId);
            var commissionRate = order!.SourcePlatform switch
            {
                PlatformType.Trendyol => 0.15m,
                PlatformType.Hepsiburada => 0.12m,
                PlatformType.N11 => 0.10m,
                PlatformType.Ciceksepeti => 0.14m,
                PlatformType.Amazon => 0.15m,
                PlatformType.Pazarama => 0.08m,
                _ => 0.10m
            };

            var grossAmount = order.SubTotal * 1.20m; // KDV dahil
            var commission = grossAmount * commissionRate;
            var netAmount = grossAmount - commission;

            var settlement = SettlementBatch.Create(
                _tenantId,
                order.SourcePlatform.ToString(),
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow,
                grossAmount,
                commission,
                netAmount);
            _context.SettlementBatches.Add(settlement);

            // BankTransaction — bankaya yatan tutar
            var bankTx = BankTransaction.Create(
                _tenantId,
                Guid.NewGuid(), // BankAccountId placeholder
                DateTime.UtcNow.AddDays(-3),
                netAmount,
                $"Hakedis: {order.ExternalOrderId}");
            _context.AccountingBankTransactions.Add(bankTx);
        }
        await _context.SaveChangesAsync();

        // FUTURE: MediatR entegrasyonu:
        // var cmd = new RunReconciliationCommand(_tenantId);
        // var result = await Mediator.Send(cmd);
        // result.AutoMatchedCount.Should().Be(10);

        // Assert — Settlement ve BankTransaction sayıları
        var settlements = await _context.SettlementBatches
            .Where(s => s.TenantId == _tenantId)
            .ToListAsync();
        settlements.Should().HaveCount(10, "10 hakedis kaydı olmalı");

        var bankTxs = await _context.AccountingBankTransactions
            .Where(b => b.TenantId == _tenantId)
            .ToListAsync();
        bankTxs.Should().HaveCount(10, "10 banka kaydı olmalı");

        // Komisyon kontrolü
        var totalCommission = settlements.Sum(s => s.TotalCommission);
        totalCommission.Should().BeGreaterThan(0, "Platform komisyonları hesaplanmalı");

        // Net tutar kontrolü: Brüt - Komisyon = Net
        settlements.Should().AllSatisfy(s =>
        {
            var expected = s.TotalGross - s.TotalCommission;
            s.TotalNet.Should().Be(expected, "TotalNet = TotalGross - TotalCommission");
        });
    }

    // ══════════════════════════════════════════════════════════════════
    // ADIM 7: AY SONU RAPOR
    // ══════════════════════════════════════════════════════════════════

    [SkippableFact, TestPriority(7)]
    public async Task Step07_MonthEndReport_ShouldProduceValidFinancials()
    {
        DockerHelper.SkipIfNoDocker();

        // FUTURE — MediatR ile muhasebe raporları:
        // var mizan = await Mediator.Send(new GetTrialBalanceQuery(_tenantId, startDate, endDate));
        // Math.Abs(mizan.TotalDebit - mizan.TotalCredit).Should().BeLessThan(0.01m);
        //
        // var bilanco = await Mediator.Send(new GetBalanceSheetQuery(_tenantId, endDate));
        // Math.Abs(bilanco.TotalAssets - (bilanco.TotalLiabilities + bilanco.TotalEquity)).Should().BeLessThan(0.01m);
        //
        // var cogs = await Mediator.Send(new GetFifoCOGSQuery(_tenantId));
        // cogs.Should().NotBeEmpty();

        // ── STOK DURUMU ──
        var remainingStock = await _context.Products
            .Where(p => p.TenantId == _tenantId)
            .SumAsync(p => p.Stock);
        remainingStock.Should().Be(ExpectedRemainingStock,
            $"Başlangıç: {TotalInitialStock}, Satılan: {TotalSoldQty}, Kalan: {ExpectedRemainingStock}");

        // ── KDV RAPORU ──
        var totalTaxCollected = await _context.Invoices
            .Where(i => i.TenantId == _tenantId)
            .SumAsync(i => i.TaxTotal);
        totalTaxCollected.Should().BeGreaterThan(0, "Toplanan KDV > 0 olmalı");

        // ── TOPLAM SATIŞ ──
        var totalRevenue = await _context.Invoices
            .Where(i => i.TenantId == _tenantId)
            .SumAsync(i => i.GrandTotal);
        totalRevenue.Should().BeGreaterThan(0, "Toplam satış > 0");

        // ── KOMİSYON ──
        var totalCommission = await _context.SettlementBatches
            .Where(s => s.TenantId == _tenantId)
            .SumAsync(s => s.TotalCommission);
        totalCommission.Should().BeGreaterThan(0, "Toplam komisyon > 0");

        // ── NET KÂR TAHMİNİ ──
        // Satış geliri - Alış maliyeti (satılan miktar × birim maliyet) - Komisyon
        var outMovements = await _context.StockMovements
            .Where(s => s.TenantId == _tenantId && s.Quantity < 0)
            .ToListAsync();
        var totalPurchaseCost = outMovements.Sum(s => Math.Abs(s.Quantity) * s.UnitCost);

        var estimatedProfit = totalRevenue - totalPurchaseCost - totalCommission;
        estimatedProfit.Should().BeGreaterThan(0,
            "Net kâr pozitif olmalı (satış fiyatları maliyet+komisyondan yüksek)");
    }

    // ══════════════════════════════════════════════════════════════════
    // Yardımcı — SettableTenantProvider
    // ══════════════════════════════════════════════════════════════════

    private sealed class SettableTenantProvider : ITenantProvider
    {
        private Guid _tenantId = Guid.Empty;
        public Guid GetCurrentTenantId() => _tenantId;
        public void SetTenant(Guid tenantId) => _tenantId = tenantId;
    }
}
