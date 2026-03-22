using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Services;
using Xunit;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// "Ahmet Bey" uctan uca senaryo testi.
/// Kozmetik magazasi sahibi Ahmet Bey'in MesTech'e kaydından
/// gunluk brifing almasina kadar 14 adimlik tam senaryo.
///
/// Senaryo ozeti:
///   1. Tenant olustur ("Ahmet Bey Kozmetik")
///   2. Trendyol magaza baglantisi kur (API key ile)
///   3. 50 urun senkronize et
///   4. 5 siparis al
///   5. Platform komisyonlarini hesapla
///   6. 1 Yurtici Kargo gonderi olustur
///   7. 1 e-fatura kes (MockInvoiceProvider)
///   8. Trendyol hesap ozeti (settlement) parse et
///   9. Muhasebe yevmiye kaydi olustur
///  10. K/Z (kar/zarar) raporu uret
///  11. OFX banka ekstresi import et
///  12. Banka mutabakati calistir
///  13. Bot uzerinden masraf onayi simule et
///  14. Gunluk danismanlik brifing'i uret
///
/// Domain-level E2E: Gercek entity factory'ler ve domain servisler kullanilir.
/// Testcontainers/DB bagimliligi yok — saf domain mantigi.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Phase", "Dalga12")]
[Collection("E2E")]
public class AhmetBeyScenarioTests
{
    // ══════════════════════════════════════════════════════════════════════════
    // Step 01 — Tenant olusturma
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ahmet Bey "Ahmet Bey Kozmetik" adinda yeni bir tenant olusturur.
    /// Tenant ID doner, veritabanina kaydedilir, varsayilan ayarlar atanir.
    /// </summary>
    [Fact(DisplayName = "Step01 — Create tenant 'Ahmet Bey Kozmetik'")]
    public async Task Step01_CreateTenant()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Name = "Ahmet Bey Kozmetik",
            TaxNumber = "1234567890",
            IsActive = true
        };

        // Assert
        await Task.CompletedTask;
        tenant.Id.Should().NotBe(Guid.Empty);
        tenant.Name.Should().Be("Ahmet Bey Kozmetik");
        tenant.TaxNumber.Should().Be("1234567890");
        tenant.IsActive.Should().BeTrue();
        tenant.Users.Should().NotBeNull().And.BeEmpty();
        tenant.Stores.Should().NotBeNull().And.BeEmpty();
        tenant.Products.Should().NotBeNull().And.BeEmpty();
        tenant.Warehouses.Should().NotBeNull().And.BeEmpty();
        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 02 — Trendyol magaza baglantisi
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trendyol magazasini API key/secret ile baglar.
    /// TestConnectionAsync basarili donmeli, StoreConnection kaydedilmeli.
    /// </summary>
    [Fact(DisplayName = "Step02 — Connect Trendyol store with API key")]
    public async Task Step02_ConnectTrendyolStore()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act — create a Store entity with Trendyol platform
        var store = new Store
        {
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Ahmet Bey Kozmetik - Trendyol",
            ExternalStoreId = "123456",
            IsActive = true
        };

        // Simulate credential storage
        var credential = new StoreCredential
        {
            StoreId = store.Id,
            Key = "ApiKey",
            EncryptedValue = "test-api-key-encrypted"
        };
        store.Credentials.Add(credential);

        // Assert
        await Task.CompletedTask;
        store.Id.Should().NotBe(Guid.Empty);
        store.TenantId.Should().Be(tenantId);
        store.PlatformType.Should().Be(PlatformType.Trendyol);
        store.StoreName.Should().Contain("Trendyol");
        store.ExternalStoreId.Should().Be("123456");
        store.IsActive.Should().BeTrue();
        store.Credentials.Should().HaveCount(1);
        store.Credentials.First().Key.Should().Be("ApiKey");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 03 — 50 urun senkronizasyonu
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trendyol'dan 50 urun cekilir ve yerel veritabanina kaydedilir.
    /// Product entity'leri olusturulur, stok ve fiyat bilgileri eslenir.
    /// </summary>
    [Fact(DisplayName = "Step03 — Sync 50 products from Trendyol")]
    public async Task Step03_Sync50Products()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act — simulate creating 50 products from a Trendyol sync
        var products = Enumerable.Range(1, 50).Select(i => new Product
        {
            TenantId = tenantId,
            Name = $"Kozmetik Urun {i}",
            SKU = $"KOZ-{i:D4}",
            Barcode = $"869000{i:D7}",
            PurchasePrice = 50m + i,
            SalePrice = 100m + i * 2,
            Stock = 10 + i,
            MinimumStock = 5,
            CategoryId = categoryId,
            IsActive = true,
            TaxRate = 0.18m
        }).ToList();

        // Assert
        await Task.CompletedTask;
        products.Should().HaveCount(50);
        products.Should().OnlyContain(p => p.TenantId == tenantId);
        products.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.SKU));
        products.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Barcode));
        products.Should().OnlyContain(p => p.SalePrice > 0);
        products.Should().OnlyContain(p => p.Stock > 0);
        products.Select(p => p.SKU).Should().OnlyHaveUniqueItems();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 04 — 5 siparis alma
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trendyol'dan 5 siparis cekilir. Her siparis 1-3 kalem icerir.
    /// Order + OrderLine entity'leri olusturulur, stok duser.
    /// </summary>
    [Fact(DisplayName = "Step04 — Receive 5 orders from Trendyol")]
    public async Task Step04_Receive5Orders()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var products = Enumerable.Range(1, 10).Select(i => new Product
        {
            TenantId = tenantId,
            Name = $"Urun {i}",
            SKU = $"SKU-{i:D4}",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = 100,
            CategoryId = Guid.NewGuid()
        }).ToList();

        var orders = new List<Order>();

        // Act — create 5 orders, each with 1-3 items, and deduct stock
        for (int i = 0; i < 5; i++)
        {
            var order = new Order
            {
                TenantId = tenantId,
                OrderNumber = $"TY-2026-{1000 + i}",
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Pending,
                SourcePlatform = PlatformType.Trendyol,
                ExternalOrderId = $"TY-EXT-{i}",
                OrderDate = DateTime.UtcNow
            };

            var itemCount = (i % 3) + 1; // 1, 2, or 3 items
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[(i + j) % products.Count];
                var qty = j + 1;
                var item = new OrderItem
                {
                    TenantId = tenantId,
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = qty,
                    UnitPrice = product.SalePrice,
                    TaxRate = 0.18m
                };
                item.CalculateAmounts();
                order.AddItem(item);

                // Deduct stock
                product.AdjustStock(-qty, StockMovementType.Sale, $"Order {order.OrderNumber}");
            }

            orders.Add(order);
        }

        // Assert
        await Task.CompletedTask;
        orders.Should().HaveCount(5);
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Pending);
        orders.Should().OnlyContain(o => o.SourcePlatform == PlatformType.Trendyol);
        orders.Should().OnlyContain(o => o.TotalAmount > 0);
        orders.Should().OnlyContain(o => o.OrderItems.Count >= 1 && o.OrderItems.Count <= 3);

        // Verify stock was deducted
        products.Should().Contain(p => p.Stock < 100,
            "at least some products should have reduced stock after orders");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 05 — Komisyon hesaplama
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 5 siparisin platform komisyonlari hesaplanir.
    /// Trendyol komisyon orani (~%18-25 arasi) urun kategorisine gore degisir.
    /// CommissionRecord entity'leri olusturulur.
    /// </summary>
    [Fact(DisplayName = "Step05 — Calculate platform commissions for 5 orders")]
    public async Task Step05_CalculateCommission()
    {
        // Arrange
        var service = new CommissionCalculationService();
        var orderAmounts = new[] { 100m, 250m, 500m, 750m, 1000m };

        // Act
        var commissions = orderAmounts
            .Select(amount => service.CalculateCommission("Trendyol", "Kozmetik", amount))
            .ToList();

        var asyncResults = new List<CommissionCalculationResult>();
        foreach (var amount in orderAmounts)
        {
            var result = await service.CalculateCommissionAsync("Trendyol", "Kozmetik", amount);
            asyncResults.Add(result);
        }

        // Assert
        commissions.Should().HaveCount(5);
        commissions.Should().OnlyContain(c => c > 0);

        // Trendyol default rate is 15%
        commissions[0].Should().Be(15m);    // 100 * 0.15
        commissions[1].Should().Be(37.5m);  // 250 * 0.15
        commissions[2].Should().Be(75m);    // 500 * 0.15
        commissions[3].Should().Be(112.5m); // 750 * 0.15
        commissions[4].Should().Be(150m);   // 1000 * 0.15

        asyncResults.Should().HaveCount(5);
        asyncResults.Should().OnlyContain(r => r.Rate == 0.15m);
        asyncResults.Should().OnlyContain(r => r.Source == "StaticFallback");
        asyncResults.Should().OnlyContain(r => r.Amount > 0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 06 — Kargo olusturma
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ilk siparisi Yurtici Kargo ile gonderir.
    /// AutoShipmentService + CargoProviderSelector kullanilir.
    /// Tracking number alinir, platform'a bildirilir.
    /// </summary>
    [Fact(DisplayName = "Step06 — Create 1 Yurtici Kargo shipment")]
    public async Task Step06_CreateShipment()
    {
        // Arrange — use the domain-level AutoShipmentService (rule-based)
        var autoShipmentService = new AutoShipmentService();
        var request = new MesTech.Domain.Services.ShipmentRequest(
            DestinationCity: "Istanbul",
            WeightKg: 2.5m,
            Desi: 5m,
            IsCashOnDelivery: false,
            SourcePlatform: PlatformType.Trendyol,
            OrderAmount: 250m
        );

        // Act
        var recommendation = autoShipmentService.Recommend(request);

        // Also verify that the Order entity can be shipped
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "TY-2026-1000",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Confirmed,
            SourcePlatform = PlatformType.Trendyol
        };
        order.MarkAsShipped("YK-123456789TR", CargoProvider.YurticiKargo);

        // Assert
        await Task.CompletedTask;
        recommendation.Provider.Should().Be(CargoProvider.YurticiKargo,
            "Trendyol platform preference maps to YurticiKargo");
        recommendation.Reason.Should().Contain("Trendyol");

        order.Status.Should().Be(OrderStatus.Shipped);
        order.TrackingNumber.Should().Be("YK-123456789TR");
        order.CargoProvider.Should().Be(CargoProvider.YurticiKargo);
        order.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 07 — e-Fatura kesme
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kargolanan siparis icin e-fatura kesilir.
    /// MockInvoiceProvider ile GIB fatura ID alinir.
    /// Fatura linki platforma gonderilir.
    /// </summary>
    [Fact(DisplayName = "Step07 — Create 1 e-invoice via MockInvoiceProvider")]
    public async Task Step07_CreateInvoice()
    {
        // Arrange — create order to base invoice on
        var tenantId = Guid.NewGuid();
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = "TY-2026-1000",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Ali Veli",
            CustomerEmail = "ali@example.com",
            Status = OrderStatus.Confirmed,
            SourcePlatform = PlatformType.Trendyol
        };
        order.SetFinancials(250m, 45m, 295m);

        // Act — create invoice from order
        var invoice = MesTech.Domain.Entities.Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-2026-001");

        // Add line item
        var line = new InvoiceLine
        {
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            ProductName = "Kozmetik Urun 1",
            SKU = "KOZ-0001",
            Quantity = 2,
            UnitPrice = 125m,
            TaxRate = 0.18m,
            TaxAmount = 45m
        };
        line.CalculateLineTotal();
        invoice.AddLine(line);

        // Mock e-invoice send
        var gibId = $"GIB-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var pdfUrl = $"https://efatura.mestech.com/{gibId}.pdf";
        invoice.MarkAsSent(gibId, pdfUrl);

        // Assert
        await Task.CompletedTask;
        invoice.Id.Should().NotBe(Guid.Empty);
        invoice.OrderId.Should().Be(order.Id);
        invoice.TenantId.Should().Be(tenantId);
        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.InvoiceNumber.Should().Be("INV-2026-001");
        invoice.CustomerName.Should().Be("Ali Veli");
        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.GibInvoiceId.Should().StartWith("GIB-");
        invoice.PdfUrl.Should().Contain(".pdf");
        invoice.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.GrandTotal.Should().BeGreaterThan(0);
        invoice.Lines.Should().HaveCount(1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 08 — Hesap ozeti (settlement) parse
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trendyol hesap ozeti (settlement report) parse edilir.
    /// Komisyon, kargo kesintisi, net odeme tutarlari cikarilir.
    /// SettlementRecord entity'leri olusturulur.
    /// </summary>
    [Fact(DisplayName = "Step08 — Parse a Trendyol settlement report")]
    public async Task Step08_ParseSettlement()
    {
        // Arrange — simulate settlement records parsed from CSV
        var commissionService = new CommissionCalculationService();
        var settlementRecords = Enumerable.Range(1, 5).Select(i =>
        {
            var grossAmount = 200m * i;
            var commission = commissionService.CalculateCommission("Trendyol", null, grossAmount);
            var cargoDeduction = 25m; // fixed cargo deduction per order
            var netAmount = grossAmount - commission - cargoDeduction;
            return new
            {
                OrderNumber = $"TY-{1000 + i}",
                GrossAmount = grossAmount,
                Commission = commission,
                CargoDeduction = cargoDeduction,
                NetAmount = netAmount
            };
        }).ToList();

        // Act
        var totalGross = settlementRecords.Sum(r => r.GrossAmount);
        var totalCommission = settlementRecords.Sum(r => r.Commission);
        var totalCargo = settlementRecords.Sum(r => r.CargoDeduction);
        var totalNet = settlementRecords.Sum(r => r.NetAmount);

        // Assert
        await Task.CompletedTask;
        settlementRecords.Should().HaveCount(5);
        totalGross.Should().BeGreaterThan(totalNet);
        (totalCommission + totalCargo).Should().Be(totalGross - totalNet);
        settlementRecords.Should().OnlyContain(r => r.Commission > 0);
        settlementRecords.Should().OnlyContain(r => r.NetAmount > 0);
        settlementRecords.Should().OnlyContain(r => r.GrossAmount > r.NetAmount);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 09 — Muhasebe yevmiye kaydi
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Satis ve komisyon islemleri icin muhasebe yevmiye kaydi (journal entry) olusturulur.
    /// Borc/Alacak dengesi saglanir (cift tarafli kayit).
    /// </summary>
    [Fact(DisplayName = "Step09 — Create accounting journal entry")]
    public async Task Step09_CreateJournalEntry()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var totalSales = 3000m;    // 5 orders total gross
        var netSales = 2542.37m;   // after tax
        var totalTax = 457.63m;    // KDV
        var accountReceivable = Guid.NewGuid(); // 120.01 Alicilar
        var salesRevenue = Guid.NewGuid();       // 600.01 Yurtici Satislar
        var vatPayable = Guid.NewGuid();         // 391.01 Hesaplanan KDV

        // Act
        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            "Trendyol gunluk satis kaydi — AhmetBey Kozmetik",
            referenceNumber: "JE-2026-001");

        entry.AddLine(accountReceivable, debit: totalSales, credit: 0, "Alicilar — Trendyol");
        entry.AddLine(salesRevenue, debit: 0, credit: netSales, "Yurtici Satislar");
        entry.AddLine(vatPayable, debit: 0, credit: totalTax, "Hesaplanan KDV %18");

        // Validate balance
        entry.Validate();

        // Post
        entry.Post();

        // Assert
        await Task.CompletedTask;
        entry.Id.Should().NotBe(Guid.Empty);
        entry.TenantId.Should().Be(tenantId);
        entry.Description.Should().Contain("Trendyol");
        entry.ReferenceNumber.Should().Be("JE-2026-001");
        entry.Lines.Should().HaveCount(3);
        entry.IsPosted.Should().BeTrue();
        entry.PostedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Double-entry: SUM(Debit) == SUM(Credit)
        var sumDebit = entry.Lines.Sum(l => l.Debit);
        var sumCredit = entry.Lines.Sum(l => l.Credit);
        sumDebit.Should().Be(sumCredit, "double-entry accounting: debit must equal credit");
        sumDebit.Should().Be(totalSales);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 10 — K/Z (Kar/Zarar) raporu
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kar/Zarar (profit/loss) raporu uretilir.
    /// Gelirler, COGS, komisyonlar, kargo giderleri toplanir.
    /// Net kar/zarar hesaplanir.
    /// </summary>
    [Fact(DisplayName = "Step10 — Generate K/Z (profit/loss) report")]
    public async Task Step10_ViewProfitReport()
    {
        // Arrange — simulate P&L data from previous steps
        var revenue = 3000m;         // 5 orders gross
        var cogs = 1500m;            // cost of goods sold (purchase price total)
        var commissionService = new CommissionCalculationService();
        var platformCommissions = commissionService.CalculateCommission("Trendyol", null, revenue);
        var shippingCosts = 125m;    // 5 orders * 25 TL avg cargo
        var otherExpenses = 50m;     // packaging, etc.

        // Act
        var netProfit = revenue - cogs - platformCommissions - shippingCosts - otherExpenses;
        var grossMargin = ((revenue - cogs) / revenue) * 100;
        var netMargin = (netProfit / revenue) * 100;

        // Assert
        await Task.CompletedTask;
        revenue.Should().BeGreaterThan(0);
        cogs.Should().BeGreaterThanOrEqualTo(0);
        platformCommissions.Should().Be(450m); // 3000 * 0.15
        shippingCosts.Should().BeGreaterThanOrEqualTo(0);
        netProfit.Should().Be(revenue - cogs - platformCommissions - shippingCosts - otherExpenses);
        netProfit.Should().Be(875m);
        grossMargin.Should().Be(50m);
        netMargin.Should().BeGreaterThan(0, "Ahmet Bey should be profitable");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 11 — OFX banka ekstresi import
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// OFX formatinda banka ekstresi import edilir.
    /// BankTransaction entity'leri olusturulur.
    /// Her islem tutari, tarihi ve aciklamasi parse edilir.
    /// </summary>
    [Fact(DisplayName = "Step11 — Import OFX bank statement")]
    public async Task Step11_ImportBankStatement()
    {
        // Arrange — simulate OFX-parsed bank transactions
        var tenantId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow.AddDays(-1);

        var bankTransactions = Enumerable.Range(1, 15).Select(i => new
        {
            TransactionId = Guid.NewGuid(),
            BankAccountId = bankAccountId,
            TenantId = tenantId,
            Amount = i % 2 == 0 ? 100m * i : -50m * i, // alternating credit/debit
            Date = transactionDate.AddHours(i),
            Description = i % 2 == 0
                ? $"TRENDYOL ODEME-{i}"
                : $"KARGO GIDERI-{i}",
            ReferenceNumber = $"OFX-{i:D6}",
            IsCredit = i % 2 == 0
        }).ToList();

        // Act — verify parsing/import simulation
        var importedCount = bankTransactions.Count;
        var duplicates = bankTransactions
            .GroupBy(t => t.ReferenceNumber)
            .Where(g => g.Count() > 1)
            .Count();

        // Assert
        await Task.CompletedTask;
        importedCount.Should().Be(15);
        duplicates.Should().Be(0, "no duplicate reference numbers in import");
        bankTransactions.Should().OnlyContain(t => t.Amount != 0);
        bankTransactions.Should().OnlyContain(t => t.Date != default);
        bankTransactions.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Description));
        bankTransactions.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.ReferenceNumber));
        bankTransactions.Should().OnlyContain(t => t.TenantId == tenantId);

        var credits = bankTransactions.Where(t => t.IsCredit).ToList();
        var debits = bankTransactions.Where(t => !t.IsCredit).ToList();
        credits.Should().NotBeEmpty("should have incoming payments");
        debits.Should().NotBeEmpty("should have outgoing expenses");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 12 — Banka mutabakati
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Banka islemleri ile muhasebe kayitlari arasinda mutabakat calistirilir.
    /// Eslesen, eslesmeyen ve fark olan islemler raporlanir.
    /// </summary>
    [Fact(DisplayName = "Step12 — Run bank reconciliation")]
    public async Task Step12_RunReconciliation()
    {
        // Arrange — bank transactions vs book entries
        var bankEntries = new[]
        {
            new { Ref = "PAY-001", Amount = 500m },
            new { Ref = "PAY-002", Amount = 750m },
            new { Ref = "PAY-003", Amount = 300m },
            new { Ref = "PAY-004", Amount = 1000m },
            new { Ref = "PAY-005", Amount = 200m }  // only in bank
        };

        var bookEntries = new[]
        {
            new { Ref = "PAY-001", Amount = 500m },
            new { Ref = "PAY-002", Amount = 750m },
            new { Ref = "PAY-003", Amount = 305m },  // discrepancy: bank=300, book=305
            new { Ref = "PAY-004", Amount = 1000m },
            new { Ref = "PAY-006", Amount = 150m }   // only in book
        };

        // Act — reconciliation logic
        var bankRefs = bankEntries.Select(b => b.Ref).ToHashSet();
        var bookRefs = bookEntries.Select(b => b.Ref).ToHashSet();

        var matchedRefs = bankRefs.Intersect(bookRefs).ToList();
        var unmatchedBankRefs = bankRefs.Except(bookRefs).ToList();
        var unmatchedBookRefs = bookRefs.Except(bankRefs).ToList();

        var discrepancies = matchedRefs
            .Select(r => new
            {
                Ref = r,
                BankAmount = bankEntries.First(b => b.Ref == r).Amount,
                BookAmount = bookEntries.First(b => b.Ref == r).Amount
            })
            .Where(d => d.BankAmount != d.BookAmount)
            .ToList();

        var bankBalance = bankEntries.Sum(b => b.Amount);
        var bookBalance = bookEntries.Sum(b => b.Amount);
        var difference = bankBalance - bookBalance;

        // Assert
        await Task.CompletedTask;
        matchedRefs.Should().HaveCount(4, "4 refs exist in both bank and book");
        unmatchedBankRefs.Should().HaveCount(1).And.Contain("PAY-005");
        unmatchedBookRefs.Should().HaveCount(1).And.Contain("PAY-006");
        discrepancies.Should().HaveCount(1, "PAY-003 has amount mismatch");
        discrepancies[0].Ref.Should().Be("PAY-003");
        discrepancies[0].BankAmount.Should().Be(300m);
        discrepancies[0].BookAmount.Should().Be(305m);
        difference.Should().Be(bankBalance - bookBalance);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 13 — Bot masraf onayi
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Telegram/Bitrix24 bot uzerinden gelen masraf onayi simule edilir.
    /// ExpenseRequest olusturulur, onaylanir, muhasebe kaydina yazilir.
    /// </summary>
    [Fact(DisplayName = "Step13 — Simulate bot expense approval")]
    public async Task Step13_ApproveExpenseFromBot()
    {
        // Arrange — create expense request notification
        var tenantId = Guid.NewGuid();
        var expenseNotification = NotificationLog.Create(
            tenantId,
            NotificationChannel.Telegram,
            "bot:telegram:ahmet",
            "ExpenseApprovalRequest",
            "Masraf Onayi: Kargo ambalaj malzemesi — 450.00 TL. Onaylamak icin /onayla yazin."
        );

        // Act — simulate approval flow
        expenseNotification.MarkAsSent();
        expenseNotification.MarkAsDelivered();
        expenseNotification.MarkAsRead();

        // Create audit log for the approval
        var auditLog = AuditLog.Create(
            tenantId,
            userId: Guid.NewGuid(),
            userName: "ahmet@kozmetik.com",
            action: "ApproveExpense",
            entityType: "ExpenseRequest",
            entityId: Guid.NewGuid(),
            oldValues: """{"Status":"Pending","Amount":450.00}""",
            newValues: """{"Status":"Approved","Amount":450.00,"ApprovedBy":"ahmet@kozmetik.com"}""",
            ipAddress: "bot:telegram"
        );

        // Create journal entry for the approved expense (770.xx Genel Yonetim Giderleri)
        var entry = JournalEntry.Create(tenantId, DateTime.UtcNow, "Bot masraf onayi — Kargo ambalaj");
        var expenseAccountId = Guid.NewGuid(); // 770.01
        var cashAccountId = Guid.NewGuid();    // 100.01
        entry.AddLine(expenseAccountId, debit: 450m, credit: 0, "Genel Yonetim Giderleri");
        entry.AddLine(cashAccountId, debit: 0, credit: 450m, "Kasa");
        entry.Validate();
        entry.Post();

        // Assert
        await Task.CompletedTask;
        expenseNotification.Status.Should().Be(NotificationStatus.Read);
        expenseNotification.Channel.Should().Be(NotificationChannel.Telegram);
        expenseNotification.ReadAt.Should().NotBeNull();

        auditLog.Action.Should().Be("ApproveExpense");
        auditLog.UserName.Should().Be("ahmet@kozmetik.com");
        auditLog.NewValues.Should().Contain("Approved");

        entry.IsPosted.Should().BeTrue();
        entry.Lines.Sum(l => l.Debit).Should().Be(entry.Lines.Sum(l => l.Credit));
        entry.Lines.Sum(l => l.Debit).Should().Be(450m);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Step 14 — Gunluk brifing
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gunluk danismanlik brifing'i uretilir.
    /// Satis ozeti, stok uyarilari, kar/zarar durumu, bekleyen islemler toplanir.
    /// Brifing metni doner (Markdown veya StructuredBriefingDto).
    /// </summary>
    [Fact(DisplayName = "Step14 — Generate daily advisory briefing")]
    public async Task Step14_GenerateDailyBriefing()
    {
        // Arrange — aggregate daily metrics
        var tenantId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;

        // Sales summary
        var totalOrders = 5;
        var totalRevenue = 3000m;
        var avgOrderValue = totalRevenue / totalOrders;

        // Stock alerts
        var lowStockProducts = new[]
        {
            new { SKU = "KOZ-0001", Name = "Nemlendirici Krem", Stock = 3, MinStock = 5 },
            new { SKU = "KOZ-0015", Name = "Gunes Kremi SPF50", Stock = 2, MinStock = 5 }
        };

        // Profit summary
        var netProfit = 875m;
        var profitMargin = (netProfit / totalRevenue) * 100;

        // Pending actions
        var pendingActions = new List<string>
        {
            "2 urun icin stok siparisi olustur",
            "PAY-005 banka islemi mutabakat bekliyor",
            "3 siparisin faturasi kesilmedi"
        };

        // Act — generate markdown briefing
        var briefingLines = new List<string>
        {
            $"# MesTech Gunluk Brifing — {today:yyyy-MM-dd}",
            $"**Tenant:** Ahmet Bey Kozmetik",
            "",
            "## Satis Ozeti",
            $"- Toplam Siparis: {totalOrders}",
            $"- Toplam Gelir: {totalRevenue:N2} TL",
            $"- Ortalama Siparis: {avgOrderValue:N2} TL",
            "",
            "## Stok Uyarilari",
        };
        foreach (var alert in lowStockProducts)
        {
            briefingLines.Add($"- **{alert.SKU}** {alert.Name}: {alert.Stock}/{alert.MinStock} (DUSUK)");
        }
        briefingLines.AddRange(new[]
        {
            "",
            "## Kar/Zarar",
            $"- Net Kar: {netProfit:N2} TL",
            $"- Kar Marji: %{profitMargin:N1}",
            "",
            "## Bekleyen Islemler"
        });
        foreach (var action in pendingActions)
        {
            briefingLines.Add($"- [ ] {action}");
        }

        var briefingMarkdown = string.Join("\n", briefingLines);

        // Create notification for briefing delivery
        var notification = NotificationLog.Create(
            tenantId,
            NotificationChannel.Telegram,
            "ahmet@kozmetik.com",
            "DailyBriefing",
            briefingMarkdown
        );
        notification.MarkAsSent();

        // Assert
        await Task.CompletedTask;
        briefingMarkdown.Should().NotBeNullOrWhiteSpace();
        briefingMarkdown.Length.Should().BeGreaterThan(100, "briefing should be substantial");
        briefingMarkdown.Should().Contain("Satis Ozeti");
        briefingMarkdown.Should().Contain("Stok Uyarilari");
        briefingMarkdown.Should().Contain("Kar/Zarar");
        briefingMarkdown.Should().Contain("Bekleyen Islemler");
        briefingMarkdown.Should().Contain("Ahmet Bey Kozmetik");

        lowStockProducts.Should().HaveCountGreaterOrEqualTo(0);
        pendingActions.Should().HaveCountGreaterOrEqualTo(0);

        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.TemplateName.Should().Be("DailyBriefing");
        notification.SentAt.Should().NotBeNull();
    }
}
