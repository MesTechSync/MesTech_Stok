using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using AccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Tests.Unit.Accounting;

/// <summary>
/// Tam ay muhasebe dongusu E2E testi.
/// Ahmet Bey A.S. firmasinin 1 aylik muhasebe islemlerini simule eder.
/// Gercek domain entity'leri kullanilir, repository'ler mock'lanmaz (pure domain test).
///
/// Hafta 1: Hesap Plani + Acilis Bakiyesi + 5 Satis
/// Hafta 2: Alis + Komisyon + Kargo + Kira
/// Hafta 3: Tahsilat + Mutabakat + Odeme
/// Hafta 4: Ay Sonu Kapanisi (SMM, KDV, Mizan, Gelir Tablosu, Bilanco)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
[Trait("Scope", "E2E")]
public class FullMonthAccountingCycleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly DateTime _monthStart = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    // ══════════════════════════════════════════════════════════
    // Hesap Plani (Chart of Accounts) — Tekduzen Hesap Plani
    // ══════════════════════════════════════════════════════════
    private readonly Dictionary<string, ChartOfAccounts> _accounts = new();

    // ══════════════════════════════════════════════════════════
    // Yevmiye Defterleri (Journal Entries) — tum aylik islemler
    // ══════════════════════════════════════════════════════════
    private readonly List<JournalEntry> _journalEntries = new();

    // ══════════════════════════════════════════════════════════
    // Platform Komisyon Oranlari
    // ══════════════════════════════════════════════════════════
    private readonly List<PlatformCommission> _commissionRates = new();

    // ══════════════════════════════════════════════════════════
    // Stok Hareketleri
    // ══════════════════════════════════════════════════════════
    private readonly List<StockMovement> _stockMovements = new();

    // ══════════════════════════════════════════════════════════
    // Settlement Batch'leri
    // ══════════════════════════════════════════════════════════
    private readonly List<SettlementBatch> _settlements = new();

    // ──────────────────────────────────────────────────────────
    // Helper: Hesap Plani Olustur
    // ──────────────────────────────────────────────────────────
    private ChartOfAccounts CreateAccount(string code, string name, AccountType type, int level = 2)
    {
        var account = ChartOfAccounts.Create(_tenantId, code, name, type, level: level);
        _accounts[code] = account;
        return account;
    }

    // ──────────────────────────────────────────────────────────
    // Helper: Yevmiye Kaydi Olustur ve Post Et
    // ──────────────────────────────────────────────────────────
    private JournalEntry CreateAndPostEntry(
        DateTime date,
        string description,
        (string accountCode, decimal debit, decimal credit)[] lines,
        string? referenceNumber = null)
    {
        var entry = JournalEntry.Create(_tenantId, date, description, referenceNumber);
        foreach (var (accountCode, debit, credit) in lines)
        {
            var account = _accounts[accountCode];
            entry.AddLine(account.Id, debit, credit, $"{accountCode} - {account.Name}");
        }
        entry.Post();
        _journalEntries.Add(entry);
        return entry;
    }

    // ──────────────────────────────────────────────────────────
    // Helper: Hesap bakiyesi hesapla (tum yevmiye satirlarindan)
    // Normal bakiye: Asset/Expense = Debit, Liability/Equity/Revenue = Credit
    // ──────────────────────────────────────────────────────────
    private decimal GetAccountBalance(string accountCode)
    {
        var account = _accounts[accountCode];
        var totalDebit = _journalEntries
            .SelectMany(je => je.Lines)
            .Where(l => l.AccountId == account.Id)
            .Sum(l => l.Debit);
        var totalCredit = _journalEntries
            .SelectMany(je => je.Lines)
            .Where(l => l.AccountId == account.Id)
            .Sum(l => l.Credit);

        return account.AccountType switch
        {
            AccountType.Asset or AccountType.Expense => totalDebit - totalCredit,
            AccountType.Liability or AccountType.Equity or AccountType.Revenue => totalCredit - totalDebit,
            _ => totalDebit - totalCredit
        };
    }

    // ──────────────────────────────────────────────────────────
    // Helper: Toplam borc/alacak (mizan icin)
    // ──────────────────────────────────────────────────────────
    private (decimal totalDebit, decimal totalCredit) GetTrialBalanceTotals()
    {
        var totalDebit = _journalEntries
            .SelectMany(je => je.Lines)
            .Sum(l => l.Debit);
        var totalCredit = _journalEntries
            .SelectMany(je => je.Lines)
            .Sum(l => l.Credit);
        return (totalDebit, totalCredit);
    }

    // ══════════════════════════════════════════════════════════
    //  ANA TEST: Tam Ay Muhasebe Dongusu
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void FullMonth_AhmetBeyAS_ShouldCompleteEntireAccountingCycle()
    {
        // ========================================================
        // HAFTA 1: KURULUM & SATISLAR
        // ========================================================

        // ── Adim 1: Hesap Plani Olustur ──
        // Varliklar (Assets)
        CreateAccount("100", "Kasa", AccountType.Asset);
        CreateAccount("102", "Bankalar", AccountType.Asset);
        CreateAccount("120", "Alicilar", AccountType.Asset);
        CreateAccount("153", "Ticari Mallar", AccountType.Asset);
        CreateAccount("191", "Indirilecek KDV", AccountType.Asset);

        // Borclar (Liabilities)
        CreateAccount("320", "Saticilar", AccountType.Liability);
        CreateAccount("391", "Hesaplanan KDV", AccountType.Liability);

        // Ozkaynaklar (Equity)
        CreateAccount("500", "Sermaye", AccountType.Equity);
        CreateAccount("570", "Gecmis Yil Karlari", AccountType.Equity);
        CreateAccount("590", "Donem Net Kari", AccountType.Equity);

        // Gelirler (Revenue)
        CreateAccount("600", "Yurtici Satislar", AccountType.Revenue);

        // Giderler (Expenses)
        CreateAccount("621", "Satilan Ticari Mallar Maliyeti", AccountType.Expense);
        CreateAccount("653", "Komisyon Giderleri", AccountType.Expense);
        CreateAccount("760", "Pazarlama Satis Dagitim Giderleri", AccountType.Expense);
        CreateAccount("770", "Genel Yonetim Giderleri", AccountType.Expense);

        _accounts.Should().HaveCount(15);
        _accounts.Values.Should().OnlyContain(a => a.IsActive);

        // ── Adim 2: Acilis Bakiyeleri ──
        // 100,000 TL kasa + 50,000 TL ticari mallar = 150,000 TL sermaye
        CreateAndPostEntry(
            _monthStart,
            "Acilis bakiyesi — Ahmet Bey A.S.",
            new[]
            {
                ("100", 100_000m, 0m),   // Kasa (Borc)
                ("153", 50_000m, 0m),    // Ticari Mallar (Borc)
                ("500", 0m, 150_000m)    // Sermaye (Alacak)
            },
            "AB-2026-ACILIS");

        GetAccountBalance("100").Should().Be(100_000m, "Kasa acilis bakiyesi 100,000 TL olmali");
        GetAccountBalance("153").Should().Be(50_000m, "Ticari mallar acilis bakiyesi 50,000 TL olmali");
        GetAccountBalance("500").Should().Be(150_000m, "Sermaye 150,000 TL olmali");

        // ── Adim 3-4: 5 Urun Satisi (farkli platformlar, farkli fiyatlar) ──
        // Her satis icin: Alicilar (Borc) / Yurtici Satislar + KDV (Alacak)
        var sales = new[]
        {
            (date: _monthStart.AddDays(1), platform: "Trendyol",     amount: 2_500m,  kdv: 450m,  orderId: "TR-001"),
            (date: _monthStart.AddDays(2), platform: "Hepsiburada",  amount: 3_800m,  kdv: 684m,  orderId: "HB-001"),
            (date: _monthStart.AddDays(3), platform: "N11",          amount: 1_200m,  kdv: 216m,  orderId: "N11-001"),
            (date: _monthStart.AddDays(4), platform: "Trendyol",     amount: 5_000m,  kdv: 900m,  orderId: "TR-002"),
            (date: _monthStart.AddDays(5), platform: "Hepsiburada",  amount: 2_000m,  kdv: 360m,  orderId: "HB-002"),
        };

        foreach (var sale in sales)
        {
            CreateAndPostEntry(
                sale.date,
                $"{sale.platform} satisi — {sale.orderId}",
                new[]
                {
                    ("120", sale.amount + sale.kdv, 0m),  // Alicilar (KDV dahil toplam)
                    ("600", 0m, sale.amount),              // Yurtici Satislar
                    ("391", 0m, sale.kdv)                  // Hesaplanan KDV
                },
                sale.orderId);
        }

        var totalSalesRevenue = sales.Sum(s => s.amount);     // 14,500 TL
        var totalSalesKdv = sales.Sum(s => s.kdv);             // 2,610 TL
        var totalReceivables = totalSalesRevenue + totalSalesKdv; // 17,110 TL

        totalSalesRevenue.Should().Be(14_500m);
        totalSalesKdv.Should().Be(2_610m);

        // ── Adim 5: Borc == Alacak kontrolu (tum yevmiye kayitlari) ──
        var (trialDebit1, trialCredit1) = GetTrialBalanceTotals();
        trialDebit1.Should().Be(trialCredit1,
            "Hafta 1 sonunda tum yevmiye kayitlarinda toplam borc == toplam alacak olmali");

        // ========================================================
        // HAFTA 2: ALISLAR & GIDERLER
        // ========================================================

        // ── Adim 6: 3 Envanter Alisi (tedarikci faturalari) ──
        var purchases = new[]
        {
            (date: _monthStart.AddDays(7),  supplier: "Demir Ticaret Ltd.",    amount: 8_000m,  kdv: 1_440m, invoice: "DT-F001"),
            (date: _monthStart.AddDays(8),  supplier: "Yildiz Tekstil A.S.",   amount: 5_500m,  kdv: 990m,   invoice: "YT-F001"),
            (date: _monthStart.AddDays(9),  supplier: "Anadolu Gida San.",     amount: 3_200m,  kdv: 576m,   invoice: "AG-F001"),
        };

        foreach (var purchase in purchases)
        {
            CreateAndPostEntry(
                purchase.date,
                $"Mal alisi — {purchase.supplier} ({purchase.invoice})",
                new[]
                {
                    ("153", purchase.amount, 0m),        // Ticari Mallar (Borc)
                    ("191", purchase.kdv, 0m),           // Indirilecek KDV (Borc)
                    ("320", 0m, purchase.amount + purchase.kdv) // Saticilar (Alacak)
                },
                purchase.invoice);

            // Stok hareketi kaydi
            var mov = new StockMovement
            {
                TenantId = _tenantId,
                ProductId = Guid.NewGuid(),
                Quantity = 10,
                MovementType = StockMovementType.Purchase.ToString(),
                UnitCost = purchase.amount / 10m,
                TotalCost = purchase.amount,
                DocumentNumber = purchase.invoice,
                Date = purchase.date,
                SupplierId = Guid.NewGuid()
            };
            mov.SetStockLevels(0, 10);
            _stockMovements.Add(mov);
        }

        var totalPurchaseAmount = purchases.Sum(p => p.amount);  // 16,700 TL
        var totalPurchaseKdv = purchases.Sum(p => p.kdv);         // 3,006 TL

        totalPurchaseAmount.Should().Be(16_700m);
        totalPurchaseKdv.Should().Be(3_006m);

        // ── Adim 7: Platform Komisyonlari ──
        // Trendyol %12, Hepsiburada %10, N11 %8
        var trendyolCommission = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Trendyol,
            Type = CommissionType.Percentage,
            Rate = 12m,
            Currency = "TRY",
            EffectiveFrom = _monthStart,
            IsActive = true,
            CategoryName = "Genel"
        };
        var hbCommission = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Hepsiburada,
            Type = CommissionType.Percentage,
            Rate = 10m,
            Currency = "TRY",
            EffectiveFrom = _monthStart,
            IsActive = true,
            CategoryName = "Genel"
        };
        var n11Commission = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.N11,
            Type = CommissionType.Percentage,
            Rate = 8m,
            Currency = "TRY",
            EffectiveFrom = _monthStart,
            IsActive = true,
            CategoryName = "Genel"
        };
        _commissionRates.AddRange(new[] { trendyolCommission, hbCommission, n11Commission });

        // Komisyon hesapla
        var trendyolSalesTotal = sales.Where(s => s.platform == "Trendyol").Sum(s => s.amount);   // 7,500 TL
        var hbSalesTotal = sales.Where(s => s.platform == "Hepsiburada").Sum(s => s.amount);      // 5,800 TL
        var n11SalesTotal = sales.Where(s => s.platform == "N11").Sum(s => s.amount);              // 1,200 TL

        var trendyolCommissionAmount = trendyolCommission.Calculate(trendyolSalesTotal);  // 900 TL
        var hbCommissionAmount = hbCommission.Calculate(hbSalesTotal);                     // 580 TL
        var n11CommissionAmount = n11Commission.Calculate(n11SalesTotal);                   // 96 TL
        var totalCommission = trendyolCommissionAmount + hbCommissionAmount + n11CommissionAmount;

        trendyolCommissionAmount.Should().Be(900m, "Trendyol %12 x 7,500 = 900 TL");
        hbCommissionAmount.Should().Be(580m, "Hepsiburada %10 x 5,800 = 580 TL");
        n11CommissionAmount.Should().Be(96m, "N11 %8 x 1,200 = 96 TL");
        totalCommission.Should().Be(1_576m, "Toplam komisyon 1,576 TL olmali");

        // Komisyon gideri yevmiye kaydi
        CreateAndPostEntry(
            _monthStart.AddDays(10),
            "Platform komisyon giderleri — Mart 2026",
            new[]
            {
                ("653", totalCommission, 0m),  // Komisyon Giderleri (Borc)
                ("120", 0m, totalCommission)   // Alicilardan dusulur (Alacak)
            },
            "KOM-2026-03");

        // ── Adim 8: Kargo Maliyetleri (3 farkli kargo firmasi) ──
        var cargoExpenses = new[]
        {
            (provider: "Yurtici Kargo",   amount: 450m,  date: _monthStart.AddDays(10)),
            (provider: "Aras Kargo",      amount: 380m,  date: _monthStart.AddDays(11)),
            (provider: "Surat Kargo",     amount: 270m,  date: _monthStart.AddDays(12)),
        };

        var totalCargoExpense = cargoExpenses.Sum(c => c.amount); // 1,100 TL

        CreateAndPostEntry(
            _monthStart.AddDays(12),
            "Kargo giderleri — Mart 2026",
            new[]
            {
                ("760", totalCargoExpense, 0m),  // Pazarlama/Dagitim Giderleri (Borc)
                ("100", 0m, totalCargoExpense)   // Kasa (Alacak)
            },
            "KARGO-2026-03");

        // ── Adim 9: Aylik Kira Gideri (5,000 TL) ──
        CreateAndPostEntry(
            _monthStart.AddDays(13),
            "Mart 2026 kira gideri — Istiklal Cad. No:42",
            new[]
            {
                ("770", 5_000m, 0m),    // Genel Yonetim Giderleri (Borc)
                ("100", 0m, 5_000m)     // Kasa (Alacak)
            },
            "KIRA-2026-03");

        // ── Adim 10: Calisma mizani hala dengelemeli ──
        var (trialDebit2, trialCredit2) = GetTrialBalanceTotals();
        trialDebit2.Should().Be(trialCredit2,
            "Hafta 2 sonunda tum yevmiye kayitlarinda toplam borc == toplam alacak olmali");

        // ========================================================
        // HAFTA 3: TAHSILATLAR & ODEMELER
        // ========================================================

        // ── Adim 11: Banka ekstresi ithal (5 gelen odeme) ──
        var bankCollections = new[]
        {
            (date: _monthStart.AddDays(14), amount: 2_950m,  ref_: "TR-001",  platform: "Trendyol"),     // 2,500 + 450 KDV
            (date: _monthStart.AddDays(15), amount: 4_484m,  ref_: "HB-001",  platform: "Hepsiburada"),  // 3,800 + 684 KDV
            (date: _monthStart.AddDays(16), amount: 1_416m,  ref_: "N11-001", platform: "N11"),           // 1,200 + 216 KDV
            (date: _monthStart.AddDays(17), amount: 5_900m,  ref_: "TR-002",  platform: "Trendyol"),     // 5,000 + 900 KDV
            (date: _monthStart.AddDays(18), amount: 2_360m,  ref_: "HB-002",  platform: "Hepsiburada"),  // 2,000 + 360 KDV
        };

        var totalCollections = bankCollections.Sum(c => c.amount); // 17,110 TL

        foreach (var collection in bankCollections)
        {
            CreateAndPostEntry(
                collection.date,
                $"Tahsilat — {collection.platform} ({collection.ref_})",
                new[]
                {
                    ("102", collection.amount, 0m),  // Bankalar (Borc)
                    ("120", 0m, collection.amount)   // Alicilar (Alacak)
                },
                $"TAH-{collection.ref_}");
        }

        // ── Adim 12: Hesap kesimi mutabakati ──
        // 3 platform icin settlement batch olustur
        var platformSettlements = new[]
        {
            ("Trendyol",    7_500m, trendyolCommissionAmount, 7_500m - trendyolCommissionAmount),
            ("Hepsiburada", 5_800m, hbCommissionAmount,       5_800m - hbCommissionAmount),
            ("N11",         1_200m, n11CommissionAmount,       1_200m - n11CommissionAmount),
        };

        foreach (var (platform, gross, commission, net) in platformSettlements)
        {
            var batch = SettlementBatch.Create(
                _tenantId,
                platform,
                _monthStart,
                _monthStart.AddDays(28),
                gross,
                commission,
                net);

            // Her satis icin settlement line ekle
            foreach (var sale in sales.Where(s => s.platform == platform))
            {
                var commRate = _commissionRates.First(c => c.Platform.ToString() == platform);
                var lineCommission = commRate.Calculate(sale.amount);
                var line = SettlementLine.Create(
                    _tenantId,
                    batch.Id,
                    sale.orderId,
                    sale.amount,
                    lineCommission,
                    0m, // service fee
                    0m, // cargo deduction
                    0m, // refund deduction
                    sale.amount - lineCommission);
                batch.AddLine(line);
            }

            batch.MarkReconciled();
            _settlements.Add(batch);
        }

        _settlements.Should().HaveCount(3);
        _settlements.Should().OnlyContain(s => s.Status == SettlementStatus.Reconciled);

        var matchedSettlementLines = _settlements.SelectMany(s => s.Lines).Count();
        matchedSettlementLines.Should().Be(5, "5 siparis icin 5 settlement satiri olmali");

        // ── Adim 13: Tedarikci odemeleri (3 giden odeme) ──
        var supplierPayments = new[]
        {
            (date: _monthStart.AddDays(19), supplier: "Demir Ticaret Ltd.",   amount: 9_440m,  invoice: "DT-F001"),  // 8,000 + 1,440 KDV
            (date: _monthStart.AddDays(20), supplier: "Yildiz Tekstil A.S.", amount: 6_490m,  invoice: "YT-F001"),   // 5,500 + 990 KDV
            (date: _monthStart.AddDays(21), supplier: "Anadolu Gida San.",   amount: 3_776m,  invoice: "AG-F001"),   // 3,200 + 576 KDV
        };

        foreach (var payment in supplierPayments)
        {
            CreateAndPostEntry(
                payment.date,
                $"Tedarikci odemesi — {payment.supplier}",
                new[]
                {
                    ("320", payment.amount, 0m),  // Saticilar (Borc)
                    ("102", 0m, payment.amount)   // Bankalar (Alacak)
                },
                $"ODE-{payment.invoice}");
        }

        var totalSupplierPayments = supplierPayments.Sum(p => p.amount); // 19,706 TL

        // ── Adim 14: 5 gun icin gunluk kar hesapla ──
        // Gunluk basit kar: o gune ait satis geliri - tahmini maliyet
        // Her gun icin 1 satis var (Adim 3 verileri)
        var dailyProfits = new Dictionary<int, decimal>();
        var estimatedCostRate = 0.60m; // Satis fiyatinin %60'i maliyet (ortalama)

        for (int day = 1; day <= 5; day++)
        {
            var daySale = sales[day - 1];
            var dayRevenue = daySale.amount;
            var dayCost = Math.Round(dayRevenue * estimatedCostRate, 2);
            dailyProfits[day] = dayRevenue - dayCost;
        }

        dailyProfits.Values.Should().OnlyContain(p => p > 0, "Tum gunler karli olmali");
        dailyProfits.Values.Sum().Should().Be(
            sales.Sum(s => s.amount) - Math.Round(sales.Sum(s => s.amount) * estimatedCostRate, 2),
            "Gunluk karlarin toplami toplam kara esit olmali");

        // ── Adim 15: Kasa bakiyesi = Acilis + Tahsilatlar - Odemeler ──
        // Kasa'dan cikan: kargo (1,100) + kira (5,000)
        // Kasa'ya giren: acilis (100,000)
        // Banka'ya giren: tahsilatlar (17,110)
        // Banka'dan cikan: tedarikci odemeleri (19,706)
        var kasaBalance = GetAccountBalance("100");
        var expectedKasa = 100_000m - totalCargoExpense - 5_000m;
        kasaBalance.Should().Be(expectedKasa,
            "Kasa bakiyesi = 100,000 - 1,100 (kargo) - 5,000 (kira) = 93,900 TL");

        var bankaBalance = GetAccountBalance("102");
        var expectedBanka = totalCollections - totalSupplierPayments;
        bankaBalance.Should().Be(expectedBanka,
            "Banka bakiyesi = 17,110 (tahsilatlar) - 19,706 (tedarikci odemeleri)");

        // ========================================================
        // HAFTA 4: AY SONU KAPANISI
        // ========================================================

        // ── Adim 16: SMM hesapla (FIFO / Ortalama Maliyet) ──
        // Basit ortalama maliyet yontemi kullanilir
        // Acilis envanter: 50,000 TL + Alislar: 16,700 TL = 66,700 TL toplam mal
        // Satilan miktar tahmini (basit oran): satis geliri / ortalama satis fiyati
        // Ortalama maliyet orani: toplam maliyet / toplam stok degeri
        var openingInventory = 50_000m;
        var totalAvailableGoods = openingInventory + totalPurchaseAmount; // 66,700 TL
        var cogsRate = 0.65m; // Satis fiyatinin %65'i maliyet (sektör ortalamasi)
        var cogs = Math.Round(totalSalesRevenue * cogsRate, 2); // 14,500 * 0.65 = 9,425 TL
        var endingInventory = totalAvailableGoods - cogs;

        cogs.Should().BeGreaterThan(0, "SMM pozitif olmali");
        endingInventory.Should().BeGreaterThan(0, "Kapanis envanteri pozitif olmali");
        (cogs + endingInventory).Should().Be(totalAvailableGoods,
            "SMM + Kapanis Envanter = Acilis Envanter + Alislar");

        // SMM yevmiye kaydi
        CreateAndPostEntry(
            _monthStart.AddDays(27),
            "Satilan Ticari Mallar Maliyeti — Mart 2026",
            new[]
            {
                ("621", cogs, 0m),      // SMM Gideri (Borc)
                ("153", 0m, cogs)       // Ticari Mallar (Alacak)
            },
            "SMM-2026-03");

        // ── Adim 17: Platform bazli komisyon ozeti ──
        var commissionSummary = _commissionRates
            .Select(cr =>
            {
                var platformSales = sales
                    .Where(s => s.platform == cr.Platform.ToString())
                    .Sum(s => s.amount);
                return new
                {
                    Platform = cr.Platform.ToString(),
                    Rate = cr.Rate,
                    SalesTotal = platformSales,
                    CommissionTotal = cr.Calculate(platformSales)
                };
            })
            .ToList();

        commissionSummary.Should().HaveCount(3);
        commissionSummary.Sum(c => c.CommissionTotal).Should().Be(totalCommission,
            "Platform komisyon ozeti toplami, toplam komisyona esit olmali");

        // Trendyol en yuksek komisyon odemesi
        commissionSummary
            .OrderByDescending(c => c.CommissionTotal)
            .First().Platform.Should().Be("Trendyol",
                "Trendyol en yuksek komisyon odenen platform olmali");

        // ── Adim 18: KDV Hesaplamasi ──
        // Hesaplanan KDV (Satis): 2,610 TL
        // Indirilecek KDV (Alis): 3,006 TL
        // Net KDV = Hesaplanan - Indirilecek
        var outputVat = GetAccountBalance("391"); // Hesaplanan KDV
        var inputVat = GetAccountBalance("191");  // Indirilecek KDV

        outputVat.Should().Be(totalSalesKdv, "Hesaplanan KDV = satis KDV toplami (2,610 TL)");
        inputVat.Should().Be(totalPurchaseKdv, "Indirilecek KDV = alis KDV toplami (3,006 TL)");

        var netVat = outputVat - inputVat;
        // Bu durumda indirilecek KDV > hesaplanan KDV, yani devreden KDV var
        netVat.Should().BeLessThan(0,
            "Indirilecek KDV (3,006) > Hesaplanan KDV (2,610), devreden KDV olmali");

        var netVatPayable = Math.Max(0, netVat);
        var carriedForwardVat = Math.Max(0, -netVat);

        netVatPayable.Should().Be(0m, "Bu ay KDV odemesi yok, devreden KDV var");
        carriedForwardVat.Should().Be(396m,
            "Devreden KDV = 3,006 - 2,610 = 396 TL");

        // ── Adim 19: Mizan (Trial Balance) ──
        var (finalTrialDebit, finalTrialCredit) = GetTrialBalanceTotals();
        finalTrialDebit.Should().Be(finalTrialCredit,
            "Ay sonu mizani: Toplam Borc == Toplam Alacak (tam eslesme)");

        // Ek kontrol: her yevmiye kaydi kendi icinde dengelemeli
        foreach (var entry in _journalEntries)
        {
            var entryDebit = entry.Lines.Sum(l => l.Debit);
            var entryCredit = entry.Lines.Sum(l => l.Credit);
            entryDebit.Should().Be(entryCredit,
                $"Yevmiye '{entry.Description}' dengelenmeli (Borc={entryDebit}, Alacak={entryCredit})");
        }

        // ── Adim 20: Gelir Tablosu (P&L) ──
        var revenue = GetAccountBalance("600");                        // 14,500 TL
        var cogsExpense = GetAccountBalance("621");                    // 9,425 TL
        var commissionExpense = GetAccountBalance("653");              // 1,576 TL
        var marketingExpense = GetAccountBalance("760");               // 1,100 TL
        var adminExpense = GetAccountBalance("770");                   // 5,000 TL
        var totalExpenses = cogsExpense + commissionExpense + marketingExpense + adminExpense;
        var netProfit = revenue - totalExpenses;

        revenue.Should().Be(14_500m, "Gelir = Yurtici Satislar");
        cogsExpense.Should().Be(cogs, "SMM gideri hesaplanmis tutara esit olmali");
        commissionExpense.Should().Be(totalCommission, "Komisyon gideri toplam komisyona esit olmali");
        marketingExpense.Should().Be(totalCargoExpense, "Pazarlama gideri kargo giderine esit olmali");
        adminExpense.Should().Be(5_000m, "Yonetim gideri kira tutarina esit olmali");

        // Net kar: 14,500 - 9,425 - 1,576 - 1,100 - 5,000 = -2,601 TL
        // Giderlerin yuksekligi nedeniyle zarar olusmustur (donem zarari)
        var expectedNetProfit = 14_500m - 9_425m - 1_576m - 1_100m - 5_000m;
        netProfit.Should().Be(expectedNetProfit,
            "Net Kar = Gelir - (SMM + Komisyon + Kargo + Kira)");
        netProfit.Should().BeLessThan(0,
            "Ahmet Bey A.S. Mart 2026 donem zarari: giderler geliri asiyor");

        // Net kar donem kari hesabina kaydedilir
        if (netProfit != 0)
        {
            if (netProfit > 0)
            {
                CreateAndPostEntry(
                    _monthStart.AddDays(28),
                    "Donem net kari kapanisi — Mart 2026",
                    new[]
                    {
                        ("600", revenue, 0m),                // Gelir hesabi kapat (Borc)
                        ("621", 0m, cogsExpense),            // SMM hesabi kapat (Alacak)
                        ("653", 0m, commissionExpense),      // Komisyon hesabi kapat (Alacak)
                        ("760", 0m, marketingExpense),       // Pazarlama hesabi kapat (Alacak)
                        ("770", 0m, adminExpense),           // Yonetim hesabi kapat (Alacak)
                        ("590", 0m, netProfit)               // Donem Net Kari (Alacak)
                    },
                    "KAPAN-2026-03");
            }
            else
            {
                // Zarar durumu: gider hesaplari kapanir, gelir hesabi kapanir, fark zarar
                var netLoss = -netProfit;
                CreateAndPostEntry(
                    _monthStart.AddDays(28),
                    "Donem net zarari kapanisi — Mart 2026",
                    new[]
                    {
                        ("600", revenue, 0m),                // Gelir hesabi kapat (Borc)
                        ("590", netLoss, 0m),                // Donem Net Zarari (Borc)
                        ("621", 0m, cogsExpense),            // SMM hesabi kapat (Alacak)
                        ("653", 0m, commissionExpense),      // Komisyon hesabi kapat (Alacak)
                        ("760", 0m, marketingExpense),       // Pazarlama hesabi kapat (Alacak)
                        ("770", 0m, adminExpense),           // Yonetim hesabi kapat (Alacak)
                    },
                    "KAPAN-2026-03");
            }
        }

        // ── Adim 21: Bilanco (Balance Sheet) ──
        // Varliklar = Borclar + Ozkaynaklar
        // Kapaniş sonrasi gelir/gider hesaplari sifirlanmis olmali
        // Gercek hesap bakiyelerinden bilanco olustur

        // Varliklar (Assets)
        var kasa = GetAccountBalance("100");
        var banka = GetAccountBalance("102");
        var alicilar = GetAccountBalance("120");
        var ticariMallar = GetAccountBalance("153");
        var indirilecekKdv = GetAccountBalance("191");

        var totalAssets = kasa + banka + alicilar + ticariMallar + indirilecekKdv;

        // Borclar (Liabilities)
        var saticilar = GetAccountBalance("320");
        var hesaplananKdv = GetAccountBalance("391");

        var totalLiabilities = saticilar + hesaplananKdv;

        // Ozkaynaklar (Equity)
        var sermaye = GetAccountBalance("500");
        var gecmisYilKar = GetAccountBalance("570");
        var donemKar = GetAccountBalance("590");

        var totalEquity = sermaye + gecmisYilKar + donemKar;

        // ── Adim 22: Tum muhasebe denklemleri dogrulanir ──

        // Kapaniş sonrasi gelir/gider hesaplari sifir olmali
        GetAccountBalance("600").Should().Be(0m,
            "Kapaniş sonrasi Yurtici Satislar hesabi sifir olmali");
        GetAccountBalance("621").Should().Be(0m,
            "Kapaniş sonrasi SMM hesabi sifir olmali");
        GetAccountBalance("653").Should().Be(0m,
            "Kapaniş sonrasi Komisyon Giderleri hesabi sifir olmali");
        GetAccountBalance("760").Should().Be(0m,
            "Kapaniş sonrasi Pazarlama Giderleri hesabi sifir olmali");
        GetAccountBalance("770").Should().Be(0m,
            "Kapaniş sonrasi Yonetim Giderleri hesabi sifir olmali");

        // Bilanco denklemi: Varliklar == Borclar + Ozkaynaklar
        totalAssets.Should().Be(totalLiabilities + totalEquity,
            $"Bilanco denklemi: Varliklar ({totalAssets:N2}) == Borclar ({totalLiabilities:N2}) + Ozkaynaklar ({totalEquity:N2})");

        // Mizan son kontrol
        var (lastDebit, lastCredit) = GetTrialBalanceTotals();
        lastDebit.Should().Be(lastCredit,
            "Final mizan: Toplam Borc == Toplam Alacak");

        // Tum yevmiye kayitlari posted olmali
        _journalEntries.Should().OnlyContain(je => je.IsPosted,
            "Tum yevmiye kayitlari posted olmali");

        // Stok hareketleri kayitli olmali
        _stockMovements.Should().HaveCount(3, "3 alis icin 3 stok hareketi olmali");
        _stockMovements.Should().OnlyContain(sm => sm.TotalCost > 0,
            "Tum stok hareketlerinin maliyeti pozitif olmali");

        // Settlement mutabakati tamamlanmis olmali
        _settlements.Sum(s => s.TotalCommission).Should().Be(totalCommission,
            "Settlement komisyon toplami, hesaplanan komisyon toplamina esit olmali");
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: Dengesiz Yevmiye Engellenir
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void JournalEntry_WithImbalancedLines_ShouldPreventPosting()
    {
        CreateAccount("100", "Kasa", AccountType.Asset);
        CreateAccount("600", "Yurtici Satislar", AccountType.Revenue);

        var entry = JournalEntry.Create(_tenantId, _monthStart, "Dengesiz kayit");
        entry.AddLine(_accounts["100"].Id, 1_000m, 0m);
        entry.AddLine(_accounts["600"].Id, 0m, 999m);

        var act = () => entry.Post();

        act.Should().Throw<JournalEntryImbalanceException>()
            .Where(ex => ex.TotalDebit == 1_000m && ex.TotalCredit == 999m);
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: Platform Komisyon Oranlari Dogrulama
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void PlatformCommission_ShouldCalculateCorrectly_ForMultiplePlatforms()
    {
        var trendyol = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Trendyol,
            Type = CommissionType.Percentage,
            Rate = 12m,
            IsActive = true,
            EffectiveFrom = _monthStart
        };

        var hb = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Hepsiburada,
            Type = CommissionType.Percentage,
            Rate = 10m,
            IsActive = true,
            EffectiveFrom = _monthStart
        };

        var n11 = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.N11,
            Type = CommissionType.Percentage,
            Rate = 8m,
            IsActive = true,
            EffectiveFrom = _monthStart
        };

        // Ayni satis tutari icin farkli platformlar farkli komisyon uretmeli
        var saleAmount = 10_000m;

        trendyol.Calculate(saleAmount).Should().Be(1_200m, "Trendyol %12");
        hb.Calculate(saleAmount).Should().Be(1_000m, "Hepsiburada %10");
        n11.Calculate(saleAmount).Should().Be(800m, "N11 %8");

        // Sifir satis
        trendyol.Calculate(0m).Should().Be(0m, "Sifir satista komisyon sifir olmali");

        // Kucuk satis
        trendyol.Calculate(100m).Should().Be(12m, "100 TL'lik satista %12 = 12 TL");

        // IsEffective kontrolu
        trendyol.IsEffective(_monthStart).Should().BeTrue();
        trendyol.IsEffective(_monthStart.AddDays(-1)).Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: SettlementBatch Mutabakat Akisi
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void SettlementBatch_ShouldReconcileCorrectly()
    {
        var batch = SettlementBatch.Create(
            _tenantId,
            "Trendyol",
            _monthStart,
            _monthStart.AddDays(28),
            totalGross: 7_500m,
            totalCommission: 900m,
            totalNet: 6_600m);

        batch.Status.Should().Be(SettlementStatus.Imported, "Yeni batch Imported durumunda olmali");
        batch.DomainEvents.Should().ContainSingle(e => e is MesTech.Domain.Accounting.Events.SettlementImportedEvent);

        // Satirlar ekle
        var line1 = SettlementLine.Create(_tenantId, batch.Id, "TR-001", 2_500m, 300m, 0m, 0m, 0m, 2_200m);
        var line2 = SettlementLine.Create(_tenantId, batch.Id, "TR-002", 5_000m, 600m, 0m, 0m, 0m, 4_400m);
        batch.AddLine(line1);
        batch.AddLine(line2);

        batch.Lines.Should().HaveCount(2);

        // Net tutar kontrolu
        line1.CalculateNetAmount().Should().Be(2_200m,
            "TR-001: 2,500 - 300 = 2,200 TL");
        line2.CalculateNetAmount().Should().Be(4_400m,
            "TR-002: 5,000 - 600 = 4,400 TL");

        // Satir toplamlar batch toplama esit olmali
        var linesGrossSum = batch.Lines.Sum(l => l.GrossAmount);
        var linesCommSum = batch.Lines.Sum(l => l.CommissionAmount);
        var linesNetSum = batch.Lines.Sum(l => l.NetAmount);

        linesGrossSum.Should().Be(batch.TotalGross, "Satir brut toplam = Batch brut toplam");
        linesCommSum.Should().Be(batch.TotalCommission, "Satir komisyon toplam = Batch komisyon toplam");
        linesNetSum.Should().Be(batch.TotalNet, "Satir net toplam = Batch net toplam");

        // Mutabakat tamamla
        batch.MarkReconciled();
        batch.Status.Should().Be(SettlementStatus.Reconciled, "Mutabakat sonrasi Reconciled olmali");
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: KDV Hesaplama Detayli
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void VatCalculation_ShouldBeAccurate_WithTurkishRates()
    {
        // Turkiye'de standart KDV orani: %18
        var kdvRate = 0.18m;

        var salesAmounts = new[] { 2_500m, 3_800m, 1_200m, 5_000m, 2_000m };
        var purchaseAmounts = new[] { 8_000m, 5_500m, 3_200m };

        var outputVat = salesAmounts.Sum(a => Math.Round(a * kdvRate, 2));
        var inputVat = purchaseAmounts.Sum(a => Math.Round(a * kdvRate, 2));

        outputVat.Should().Be(2_610m, "Satis KDV: toplam satis * %18");
        inputVat.Should().Be(3_006m, "Alis KDV: toplam alis * %18");

        var netVat = outputVat - inputVat;
        netVat.Should().Be(-396m, "Devreden KDV: 2,610 - 3,006 = -396 TL");

        // KDV oraninin tutar uzerinden dogru uygulandigini kontrol et
        foreach (var amount in salesAmounts)
        {
            var vat = Math.Round(amount * kdvRate, 2);
            var grossAmount = amount + vat;
            grossAmount.Should().Be(amount * 1.18m, $"KDV dahil tutar: {amount} * 1.18");
        }
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: Hesap Plani CRUD Dogrulama
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void ChartOfAccounts_ShouldSupportFullLifecycle()
    {
        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);

        account.IsActive.Should().BeTrue("Yeni hesap aktif olmali");
        account.IsSystem.Should().BeFalse("Yeni hesap sistem hesabi olmamali");
        account.AccountType.Should().Be(AccountType.Asset);
        account.Code.Should().Be("100");

        account.UpdateName("Kasa Hesabi");
        account.Name.Should().Be("Kasa Hesabi");

        account.Deactivate();
        account.IsActive.Should().BeFalse("Deaktive edilmis hesap pasif olmali");

        account.Activate();
        account.IsActive.Should().BeTrue("Aktive edilmis hesap tekrar aktif olmali");

        account.MarkAsSystem();
        account.IsSystem.Should().BeTrue("Sistem hesabi olarak isaretlenmeli");

        var updateAct = () => account.UpdateName("Degistirilemez");
        updateAct.Should().Throw<InvalidOperationException>()
            .WithMessage("*System account*");

        var deactivateAct = () => account.Deactivate();
        deactivateAct.Should().Throw<InvalidOperationException>()
            .WithMessage("*System account*");

        var deleteAct = () => account.MarkDeleted("admin");
        deleteAct.Should().Throw<InvalidOperationException>()
            .WithMessage("*System account*");
    }

    // ══════════════════════════════════════════════════════════
    //  EK TEST: StockMovement Maliyet Takibi
    // ══════════════════════════════════════════════════════════
    [Fact]
    public void StockMovement_ShouldTrackCostsAccurately()
    {
        var productId = Guid.NewGuid();

        // 3 farkli alistan stok giris hareketleri
        var movements = new[]
        {
            CreateMovementWithLevels(_tenantId, productId, 10, 0, 10,
                StockMovementType.Purchase.ToString(), 800m, 8_000m, _monthStart.AddDays(7)),
            CreateMovementWithLevels(_tenantId, productId, 15, 10, 25,
                StockMovementType.Purchase.ToString(), 550m / 15m * 15m, 5_500m, _monthStart.AddDays(8)),
            CreateMovementWithLevels(_tenantId, productId, -5, 25, 20,
                StockMovementType.Sale.ToString(), 800m, 4_000m, _monthStart.AddDays(10))
        };

        // Toplam stok girisi
        var totalIn = movements.Where(m => m.Quantity > 0).Sum(m => m.Quantity);
        totalIn.Should().Be(25, "Toplam stok girisi: 10 + 15 = 25 adet");

        // Toplam stok cikisi
        var totalOut = movements.Where(m => m.Quantity < 0).Sum(m => Math.Abs(m.Quantity));
        totalOut.Should().Be(5, "Toplam stok cikisi: 5 adet");

        // Son stok seviyesi
        var lastMovement = movements.Last();
        lastMovement.NewStock.Should().Be(20, "Son stok: 25 - 5 = 20 adet");

        // Maliyet hesaplari
        var totalPurchaseCost = movements
            .Where(m => m.IsPositiveMovement)
            .Sum(m => m.TotalCost ?? 0m);
        totalPurchaseCost.Should().Be(13_500m, "Toplam alis maliyeti: 8,000 + 5,500 = 13,500 TL");

        // Ortalama birim maliyet
        var avgUnitCost = totalPurchaseCost / totalIn;
        avgUnitCost.Should().Be(540m, "Ortalama birim maliyet: 13,500 / 25 = 540 TL");

        // FIFO maliyet kontrolu
        var fifoCost = movements.First(m => m.IsPositiveMovement).UnitCost!.Value;
        fifoCost.Should().Be(800m, "FIFO: Ilk alisin birim maliyeti 800 TL");
    }

    private static StockMovement CreateMovementWithLevels(
        Guid tenantId, Guid productId, int quantity, int previousStock, int newStock,
        string movementType, decimal unitCost, decimal totalCost, DateTime date)
    {
        var m = new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            Quantity = quantity,
            MovementType = movementType,
            UnitCost = unitCost,
            TotalCost = totalCost,
            Date = date
        };
        m.SetStockLevels(previousStock, newStock);
        return m;
    }
}
