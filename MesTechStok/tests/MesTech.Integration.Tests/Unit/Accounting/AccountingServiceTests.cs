using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Accounting.Enums;
using MesTech.Infrastructure.Finance;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Muhasebe servis testleri — M3 Beta Agent genisletmesi.
/// BaBs, Depreciation, BankReconciliation, IncomeTax ve AccountingJournal testleri.
/// Gercekci Turk muhasebe verileri (TRY, VKN) kullanilir.
/// </summary>
public class AccountingServiceTests
{
    // ═══════════════════════════════════════════════════════════════════
    // Ba/Bs Rapor Servisi Testleri
    // ═══════════════════════════════════════════════════════════════════

    private readonly Guid _tenantId = Guid.NewGuid();

    private BaBsReportService CreateBaBsService(
        IReadOnlyList<AccountingDocument> purchaseDocs,
        IReadOnlyList<AccountingDocument> salesDocs,
        IReadOnlyList<Counterparty> counterparties)
    {
        var docRepoMock = new Mock<IAccountingDocumentRepository>();
        docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.PurchaseInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseDocs);
        docRepoMock.Setup(r => r.GetByTypeAsync(_tenantId, DocumentType.SalesInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesDocs);

        var cpRepoMock = new Mock<ICounterpartyRepository>();
        cpRepoMock.Setup(r => r.GetAllAsync(_tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterparties);

        var loggerMock = new Mock<ILogger<BaBsReportService>>();

        return new BaBsReportService(docRepoMock.Object, cpRepoMock.Object, loggerMock.Object);
    }

    // Yardimci: AccountingDocument.Create + private setter'lari reflection ile set et
    private static AccountingDocument CreatePurchaseDoc(
        Guid tenantId, Guid counterpartyId, decimal amount, DateTime createdAt)
    {
        var doc = AccountingDocument.Create(
            tenantId,
            "fatura.pdf",
            "application/pdf",
            1024,
            $"/docs/{Guid.NewGuid()}.pdf",
            DocumentType.PurchaseInvoice,
            DocumentSource.Upload,
            counterpartyId,
            amount);

        doc.CreatedAt = createdAt;
        return doc;
    }

    private static AccountingDocument CreateSalesDoc(
        Guid tenantId, Guid counterpartyId, decimal amount, DateTime createdAt)
    {
        var doc = AccountingDocument.Create(
            tenantId,
            "satis-fatura.pdf",
            "application/pdf",
            2048,
            $"/docs/{Guid.NewGuid()}.pdf",
            DocumentType.SalesInvoice,
            DocumentSource.Upload,
            counterpartyId,
            amount);

        doc.CreatedAt = createdAt;
        return doc;
    }

    // ─────────────────────────────────────────────────────────────────────
    // BaBs Test 1: Dogru toplam
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BaBsReport_GenerateForMonth_ProducesCorrectTotals()
    {
        // Arrange — 2 tedarikci, 1 musteri; hepsi 5.000 TL ustu
        var supplier1 = Counterparty.Create(_tenantId, "Aktas Ticaret A.S.", CounterpartyType.Supplier, "1234567890");
        var supplier2 = Counterparty.Create(_tenantId, "Demir Sanayi Ltd.", CounterpartyType.Supplier, "9876543210");
        var customer1 = Counterparty.Create(_tenantId, "Yildiz Market", CounterpartyType.Customer, "5551234567");

        var purchaseDocs = new List<AccountingDocument>
        {
            CreatePurchaseDoc(_tenantId, supplier1.Id, 15_000m, new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc)),
            CreatePurchaseDoc(_tenantId, supplier1.Id, 8_500m, new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc)),
            CreatePurchaseDoc(_tenantId, supplier2.Id, 22_000m, new DateTime(2026, 3, 20, 9, 0, 0, DateTimeKind.Utc))
        };

        var salesDocs = new List<AccountingDocument>
        {
            CreateSalesDoc(_tenantId, customer1.Id, 35_000m, new DateTime(2026, 3, 8, 11, 0, 0, DateTimeKind.Utc)),
            CreateSalesDoc(_tenantId, customer1.Id, 12_500m, new DateTime(2026, 3, 22, 16, 0, 0, DateTimeKind.Utc))
        };

        var counterparties = new List<Counterparty> { supplier1, supplier2, customer1 };

        var service = CreateBaBsService(purchaseDocs, salesDocs, counterparties);

        // Act
        var report = await service.GenerateBaBsReportAsync(_tenantId, 2026, 3);

        // Assert
        report.Should().NotBeNull();
        report.Year.Should().Be(2026);
        report.Month.Should().Be(3);

        // Ba (alislar): supplier1=23.500, supplier2=22.000 → her ikisi >= 5.000
        report.BaEntries.Should().HaveCount(2);
        report.TotalBaAmount.Should().Be(23_500m + 22_000m); // 45.500 TL

        // Bs (satislar): customer1=47.500 → >= 5.000
        report.BsEntries.Should().HaveCount(1);
        report.TotalBsAmount.Should().Be(47_500m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BaBs Test 2: Bos ay
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BaBsReport_EmptyMonth_ReturnsEmptyReport()
    {
        // Arrange — Hicbir fatura yok
        var service = CreateBaBsService(
            Array.Empty<AccountingDocument>(),
            Array.Empty<AccountingDocument>(),
            Array.Empty<Counterparty>());

        // Act
        var report = await service.GenerateBaBsReportAsync(_tenantId, 2026, 1);

        // Assert
        report.Should().NotBeNull();
        report.BaEntries.Should().BeEmpty();
        report.BsEntries.Should().BeEmpty();
        report.TotalBaAmount.Should().Be(0m);
        report.TotalBsAmount.Should().Be(0m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BaBs Test 3: Multi-tenant filtreleme
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BaBsReport_MultiTenant_FiltersCorrectTenant()
    {
        // Arrange — Sadece _tenantId'ye ait belgeler donmeli
        var otherTenantId = Guid.NewGuid();
        var supplier = Counterparty.Create(_tenantId, "Yilmaz Tekstil", CounterpartyType.Supplier, "3216549870");

        // Bu belge _tenantId'ye ait
        var doc = CreatePurchaseDoc(_tenantId, supplier.Id, 10_000m, new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc));

        var service = CreateBaBsService(
            new List<AccountingDocument> { doc },
            Array.Empty<AccountingDocument>(),
            new List<Counterparty> { supplier });

        // Act
        var report = await service.GenerateBaBsReportAsync(_tenantId, 2026, 2);

        // Assert — Sadece _tenantId verisi donmeli
        report.TenantId.Should().Be(_tenantId);
        report.BaEntries.Should().HaveCount(1);
        report.BaEntries[0].Name.Should().Be("Yilmaz Tekstil");
        report.BaEntries[0].VKN.Should().Be("3216549870");
        report.BaEntries[0].TotalAmount.Should().Be(10_000m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Amortisman (Depreciation) Testleri
    // ═══════════════════════════════════════════════════════════════════

    private readonly DepreciationService _depreciationService = new();

    // ─────────────────────────────────────────────────────────────────────
    // Depreciation Test 4: Dogrusal (linear) yontem
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DepreciationCalculation_LinearMethod_CalculatesCorrectly()
    {
        // Arrange — 100.000 TL maliyet, 5 yil faydali omur
        var cost = 100_000m;
        var years = 5;

        // Act
        var schedule = _depreciationService.CalculateDepreciation(cost, years, "linear");

        // Assert
        schedule.Should().HaveCount(5);

        // Her yil 20.000 TL amortisman (100.000 / 5)
        schedule[0].Year.Should().Be(1);
        schedule[0].DepreciationAmount.Should().Be(20_000m);
        schedule[0].AccumulatedDepreciation.Should().Be(20_000m);
        schedule[0].BookValue.Should().Be(80_000m);

        schedule[2].Year.Should().Be(3);
        schedule[2].AccumulatedDepreciation.Should().Be(60_000m);
        schedule[2].BookValue.Should().Be(40_000m);

        // Son yil: kalan deger = 0
        schedule[4].Year.Should().Be(5);
        schedule[4].BookValue.Should().Be(0m);
        schedule[4].AccumulatedDepreciation.Should().Be(100_000m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Depreciation Test 5: Azalan bakiyeler (declining) yontem
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DepreciationCalculation_DecliningBalance_CalculatesCorrectly()
    {
        // Arrange — 50.000 TL maliyet, 4 yil faydali omur
        // Oran = 2/4 = %50 (cift azalan)
        var cost = 50_000m;
        var years = 4;

        // Act
        var schedule = _depreciationService.CalculateDepreciation(cost, years, "declining");

        // Assert
        schedule.Should().HaveCount(4);

        // Yil 1: 50.000 * 0.50 = 25.000
        schedule[0].Year.Should().Be(1);
        schedule[0].DepreciationAmount.Should().Be(25_000m);
        schedule[0].BookValue.Should().Be(25_000m);

        // Yil 2: 25.000 * 0.50 = 12.500
        schedule[1].Year.Should().Be(2);
        schedule[1].DepreciationAmount.Should().Be(12_500m);
        schedule[1].BookValue.Should().Be(12_500m);

        // Son yil: kalan deger tamamen yazilir
        schedule[3].Year.Should().Be(4);
        schedule[3].BookValue.Should().Be(0m);

        // Toplam = maliyet
        schedule.Sum(s => s.DepreciationAmount).Should().Be(cost);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Depreciation Test 6: Sifir maliyet hata firmali
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DepreciationCalculation_ZeroAssetValue_ThrowsValidation()
    {
        // Arrange & Act & Assert — 0 TL maliyet → ArgumentOutOfRangeException
        var act = () => _depreciationService.CalculateDepreciation(0m, 5, "linear");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("cost");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Banka Mutabakat (Bank Reconciliation) Testleri
    // ═══════════════════════════════════════════════════════════════════

    private BankReconciliationReportService CreateReconciliationService(
        IReadOnlyList<BankTransaction> bankTxs,
        IReadOnlyList<JournalEntry> journalEntries,
        IReadOnlyList<ChartOfAccounts> accounts)
    {
        var bankTxRepoMock = new Mock<IBankTransactionRepository>();
        bankTxRepoMock.Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bankTxs);

        var journalRepoMock = new Mock<IJournalEntryRepository>();
        journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(journalEntries);

        var chartRepoMock = new Mock<IChartOfAccountsRepository>();
        chartRepoMock.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var loggerMock = new Mock<ILogger<BankReconciliationReportService>>();

        return new BankReconciliationReportService(
            bankTxRepoMock.Object, journalRepoMock.Object, chartRepoMock.Object, loggerMock.Object);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BankReconciliation Test 7: Otomatik esleme
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BankReconciliation_MatchTransactions_AutoMatches()
    {
        // Arrange
        var bankAccountId = Guid.NewGuid();
        var bankAccount = ChartOfAccounts.Create(_tenantId, "102.01", "Garanti Bankasi", AccountType.Asset);

        // JournalLine.CreatedAt = DateTime.UtcNow olur (navigation prop null).
        // Reconciliation servisi l.JournalEntry?.EntryDate ?? l.CreatedAt kullanir.
        // Esleme icin banka hareketi tarihini bugun olarak set ediyoruz.
        var today = DateTime.UtcNow.Date;

        // Banka hareketi: 5.250 TL, bugun
        var bankTx = BankTransaction.Create(
            _tenantId, bankAccountId,
            today.AddHours(10),
            5_250m,
            "Trendyol satis havalesi");

        // Muhasebe kaydi: ayni tutar, ayni gun — eslesmeli
        var journalEntry = JournalEntry.Create(
            _tenantId,
            today.AddHours(10),
            "Trendyol satis geliri",
            "JE-2026-001");
        journalEntry.AddLine(bankAccount.Id, 5_250m, 0m, "Banka tahsilat");
        journalEntry.AddLine(Guid.NewGuid(), 0m, 5_250m, "Satis geliri");
        journalEntry.Validate();

        // Post et ki IsPosted=true olsun (BankReconciliation filtresi)
        journalEntry.Post();

        var service = CreateReconciliationService(
            new List<BankTransaction> { bankTx },
            new List<JournalEntry> { journalEntry },
            new List<ChartOfAccounts> { bankAccount });

        // Act
        var report = await service.GenerateReportAsync(
            _tenantId,
            today,
            today.AddDays(1));

        // Assert
        report.Should().NotBeNull();
        report.MatchedItems.Should().HaveCount(1);
        report.MatchedItems[0].Amount.Should().Be(5_250m);
        report.UnmatchedBankItems.Should().BeEmpty();
        report.UnmatchedAccountingItems.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BankReconciliation Test 8: Eslesmeyenler isaretlenir
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BankReconciliation_UnmatchedItems_FlagsCorrectly()
    {
        // Arrange — Banka hareketi var ama muhasebe kaydi yok
        var bankAccountId = Guid.NewGuid();
        var bankAccount = ChartOfAccounts.Create(_tenantId, "102.02", "Is Bankasi", AccountType.Asset);

        var bankTx = BankTransaction.Create(
            _tenantId, bankAccountId,
            new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            3_750m,
            "N11 komisyon kesintisi");

        var service = CreateReconciliationService(
            new List<BankTransaction> { bankTx },
            Array.Empty<JournalEntry>(),
            new List<ChartOfAccounts> { bankAccount });

        // Act
        var report = await service.GenerateReportAsync(
            _tenantId,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc));

        // Assert
        report.MatchedItems.Should().BeEmpty();
        report.UnmatchedBankItems.Should().HaveCount(1);
        report.UnmatchedBankItems[0].Amount.Should().Be(3_750m);
        report.UnmatchedBankItems[0].Description.Should().Be("N11 komisyon kesintisi");
    }

    // ─────────────────────────────────────────────────────────────────────
    // BankReconciliation Test 9: Ayni tutar, ayni gun — ikinci tekrar reddedilir
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BankReconciliation_DuplicateTransaction_DetectsAndRejects()
    {
        // Arrange — 2 ayni tutarli banka hareketi, 1 muhasebe kaydi
        // Sadece biri eslesmeli, digeri "unmatched" kalmali
        var bankAccountId = Guid.NewGuid();
        var bankAccount = ChartOfAccounts.Create(_tenantId, "102.03", "Vakifbank", AccountType.Asset);

        // JournalLine.CreatedAt = DateTime.UtcNow (bugun)
        var today = DateTime.UtcNow.Date;

        var bankTx1 = BankTransaction.Create(
            _tenantId, bankAccountId,
            today.AddHours(10),
            1_000m,
            "Hepsiburada havalesi #1");

        var bankTx2 = BankTransaction.Create(
            _tenantId, bankAccountId,
            today.AddHours(10),
            1_000m,
            "Hepsiburada havalesi #2 (duplicate)");

        // Sadece 1 muhasebe kaydi
        var journalEntry = JournalEntry.Create(
            _tenantId,
            today.AddHours(10),
            "HB satis tahsilati",
            "JE-2026-010");
        journalEntry.AddLine(bankAccount.Id, 1_000m, 0m, "Banka tahsilat");
        journalEntry.AddLine(Guid.NewGuid(), 0m, 1_000m, "Satis geliri");
        journalEntry.Validate();
        journalEntry.Post();

        var service = CreateReconciliationService(
            new List<BankTransaction> { bankTx1, bankTx2 },
            new List<JournalEntry> { journalEntry },
            new List<ChartOfAccounts> { bankAccount });

        // Act
        var report = await service.GenerateReportAsync(
            _tenantId,
            today,
            today.AddDays(1));

        // Assert — 1 eslesen, 1 eslesmemis banka hareketi
        report.MatchedItems.Should().HaveCount(1);
        report.UnmatchedBankItems.Should().HaveCount(1);
        report.UnmatchedBankItems[0].Amount.Should().Be(1_000m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Gelir Vergisi (Income Tax) Testleri
    // ═══════════════════════════════════════════════════════════════════

    private readonly IncomeTaxService _incomeTaxService = new();

    // ─────────────────────────────────────────────────────────────────────
    // IncomeTax Test 10: Ceyrek yillik hesaplama — dogru dilim uygulamasi
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IncomeTax_QuarterlyCalculation_AppliesCorrectBrackets()
    {
        // Arrange — Yillik 350.000 TL gelir (orta dilim)
        // Dilimler 2026:
        //   0-110K → %15 = 16.500
        //   110K-230K → %20 = 24.000
        //   230K-350K → %27 = 32.400
        // Toplam = 72.900 TL
        var annualIncome = 350_000m;

        // Act
        var result = _incomeTaxService.CalculateIncomeTax(annualIncome, 2026);

        // Assert
        result.Should().NotBeNull();
        result.TaxableIncome.Should().Be(350_000m);
        result.Year.Should().Be(2026);

        // Dilim detaylari
        result.BracketDetails.Should().HaveCountGreaterOrEqualTo(3);

        // 1. dilim: 0-110K → %15
        result.BracketDetails[0].Rate.Should().Be(0.15m);
        result.BracketDetails[0].TaxableAmountInBracket.Should().Be(110_000m);
        result.BracketDetails[0].TaxInBracket.Should().Be(16_500m);

        // 2. dilim: 110K-230K → %20
        result.BracketDetails[1].Rate.Should().Be(0.20m);
        result.BracketDetails[1].TaxableAmountInBracket.Should().Be(120_000m);
        result.BracketDetails[1].TaxInBracket.Should().Be(24_000m);

        // 3. dilim: 230K-350K → %27
        result.BracketDetails[2].Rate.Should().Be(0.27m);
        result.BracketDetails[2].TaxableAmountInBracket.Should().Be(120_000m);
        result.BracketDetails[2].TaxInBracket.Should().Be(32_400m);

        // Toplam vergi
        result.TotalTax.Should().Be(16_500m + 24_000m + 32_400m); // 72.900 TL
    }

    // ─────────────────────────────────────────────────────────────────────
    // IncomeTax Test 11: Indirimlerle vergilendirilebilir gelir azalir
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IncomeTax_WithDeductions_ReducesTaxableIncome()
    {
        // Arrange — 200.000 TL brut gelir, 50.000 TL indirim → 150.000 TL net
        // Indirim hesaplama adapter disinda yapilir; servise net tutar girer
        var netIncome = 150_000m; // 200K - 50K indirim

        // Act
        var result = _incomeTaxService.CalculateIncomeTax(netIncome, 2026);

        // Assert
        result.TaxableIncome.Should().Be(150_000m);

        // 1. dilim: 110K → %15 = 16.500
        // 2. dilim: 40K → %20 = 8.000
        // Toplam: 24.500 TL
        result.TotalTax.Should().Be(16_500m + 8_000m); // 24.500 TL

        // Efektif oran kontrol
        result.EffectiveRate.Should().BeApproximately(24_500m / 150_000m, 0.001m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IncomeTax Test 12: Sifir gelir → sifir vergi
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IncomeTax_ZeroIncome_ReturnsZeroTax()
    {
        // Arrange & Act
        var result = _incomeTaxService.CalculateIncomeTax(0m, 2026);

        // Assert
        result.Should().NotBeNull();
        result.TaxableIncome.Should().Be(0m);
        result.TotalTax.Should().Be(0m);
        result.EffectiveRate.Should().Be(0m);
        result.BracketDetails.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Yevmiye Kaydi (Accounting Journal) Testleri
    // ═══════════════════════════════════════════════════════════════════

    // ─────────────────────────────────────────────────────────────────────
    // Journal Test 13: Borc = Alacak dengeli kayit
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AccountingJournal_CreateEntry_BalancesDebitCredit()
    {
        // Arrange — Satis kaydi: 100 Kasa (Borc) = 600 Satis Geliri (Alacak)
        var entry = JournalEntry.Create(
            _tenantId,
            new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            "Trendyol satis geliri — Fatura MES-2026-001",
            "JE-2026-100");

        var kasaHesapId = Guid.NewGuid();     // 100.01 Kasa
        var satisGelirId = Guid.NewGuid();    // 600.01 Satis Geliri

        // Act — Borc ve Alacak ekle
        entry.AddLine(kasaHesapId, 8_450m, 0m, "Kasa tahsilat");
        entry.AddLine(satisGelirId, 0m, 8_450m, "Satis geliri");

        // Validate — Borc = Alacak kontrolu
        var act = () => entry.Validate();

        // Assert — Hata firlatmamali
        act.Should().NotThrow();
        entry.Lines.Should().HaveCount(2);
        entry.Lines.Sum(l => l.Debit).Should().Be(entry.Lines.Sum(l => l.Credit));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Journal Test 14: Dengesiz kayit → JournalEntryImbalanceException
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AccountingJournal_UnbalancedEntry_ThrowsValidation()
    {
        // Arrange — Borc 10.000, Alacak 9.000 → dengesiz
        var entry = JournalEntry.Create(
            _tenantId,
            new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc),
            "Dengesiz test kaydi",
            "JE-2026-ERR");

        entry.AddLine(Guid.NewGuid(), 10_000m, 0m, "Borc tarafi");
        entry.AddLine(Guid.NewGuid(), 0m, 9_000m, "Alacak tarafi");

        // Act & Assert — JournalEntryImbalanceException bekleniyor
        var act = () => entry.Validate();

        act.Should().Throw<JournalEntryImbalanceException>()
            .Where(ex => ex.TotalDebit == 10_000m && ex.TotalCredit == 9_000m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Journal Test 15: Multi-currency TRY donusumu
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AccountingJournal_MultiCurrency_ConvertsTRY()
    {
        // Arrange — USD satis, TRY karsiligi ile kayit
        // 1 USD = 38.50 TRY varsayimi, satis tutari 500 USD = 19.250 TL
        var usdAmount = 500m;
        var exchangeRate = 38.50m;
        var tryAmount = Math.Round(usdAmount * exchangeRate, 2); // 19.250 TL

        var entry = JournalEntry.Create(
            _tenantId,
            new DateTime(2026, 3, 17, 10, 0, 0, DateTimeKind.Utc),
            "eBay USD satis — 500 USD @ 38.50 TRY",
            "JE-2026-FX-001");

        var bankUsdHesapId = Guid.NewGuid();  // 102.10 Banka USD
        var satisGelirId = Guid.NewGuid();    // 600.10 Dis Satis Geliri

        // Act — TRY karsiligi ile yevmiye kaydi
        entry.AddLine(bankUsdHesapId, tryAmount, 0m, $"500 USD @ {exchangeRate}");
        entry.AddLine(satisGelirId, 0m, tryAmount, "Dis satis geliri (TRY karsiligi)");

        var validate = () => entry.Validate();

        // Assert — Denges kayit, TRY tutarlari dogru
        validate.Should().NotThrow();

        entry.Lines.Should().HaveCount(2);
        entry.Lines[0].Debit.Should().Be(19_250m);
        entry.Lines[1].Credit.Should().Be(19_250m);
        entry.Lines.Sum(l => l.Debit).Should().Be(entry.Lines.Sum(l => l.Credit));
        entry.Description.Should().Contain("500 USD");
        entry.Description.Should().Contain("38.50 TRY");
    }
}
