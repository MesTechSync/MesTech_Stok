using System.Globalization;
using System.Text;
using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Accounting;

/// <summary>
/// C-M4-05 — TaxPrep KDV Draft Report.
/// Simulates generating a monthly KDV (VAT) draft report for March 2026.
///
/// Scenario:
///   - 50 sales:     total 100,000 TL + 18,000 TL KDV (%18)
///   - 20 purchases: total  40,000 TL +  7,200 TL KDV input (%18)
///   - 5 returns:    total   5,000 TL +    900 TL KDV refund (%18)
///   - Commission KDV from 3 platforms (Trendyol, HB, N11)
///
/// Turkish tax context:
///   Output VAT (Hesaplanan KDV) = Sales KDV - Return KDV adjustment
///   Input VAT (Indirilecek KDV) = Purchase KDV + Commission KDV
///   Payable VAT (Odenecek KDV)  = Output VAT - Input VAT
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "KdvDraft")]
[Trait("Phase", "Dalga14")]
public class TaxPrepKdvDraftTests
{
    // ── Constants ──
    private const decimal KdvRate = 0.18m;
    private const string Period = "2026-03"; // Mart 2026

    // ── Sales data: 50 sales, total 100,000 TL net ──
    private const int SaleCount = 50;
    private const decimal TotalSalesNet = 100_000m;
    private const decimal TotalSalesKdv = 18_000m; // 100,000 * 0.18

    // ── Purchase data: 20 purchases, total 40,000 TL net ──
    private const int PurchaseCount = 20;
    private const decimal TotalPurchasesNet = 40_000m;
    private const decimal TotalPurchasesKdv = 7_200m; // 40,000 * 0.18

    // ── Return data: 5 returns, total 5,000 TL net ──
    private const int ReturnCount = 5;
    private const decimal TotalReturnsNet = 5_000m;
    private const decimal TotalReturnsKdv = 900m; // 5,000 * 0.18

    // ── Commission data (3 platforms) ──
    //    Trendyol: 60,000 TL satış * %15 = 9,000 TL komisyon
    //    HB:       25,000 TL satış * %18 = 4,500 TL komisyon
    //    N11:      15,000 TL satış * %12 = 1,800 TL komisyon
    //    Total commission: 15,300 TL
    //    Commission KDV: 15,300 * 0.18 = 2,754 TL
    private static readonly List<PlatformCommissionData> PlatformCommissions = new()
    {
        new("Trendyol", 60_000m, 0.15m),
        new("Hepsiburada", 25_000m, 0.18m),
        new("N11", 15_000m, 0.12m),
    };

    private record PlatformCommissionData(string Platform, decimal SalesAmount, decimal CommissionRate);

    /// <summary>
    /// Full KDV draft report data model.
    /// </summary>
    private record KdvDraftReport(
        string Period,
        // Output (Hesaplanan)
        decimal SalesKdv,
        decimal ReturnKdvAdjustment,
        decimal NetOutputKdv,
        // Input (Indirilecek)
        decimal PurchaseKdv,
        decimal CommissionKdv,
        decimal NetInputKdv,
        // Payable
        decimal PayableKdv,
        // Breakdown text
        string ReportText);

    // ─────────────────────────────────────────────────────────────
    // Test 1: KdvDraft_OutputVat_CalculatedCorrectly
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_OutputVat_CalculatedCorrectly()
    {
        // Arrange
        var report = GenerateKdvDraftReport();

        // Assert — Output KDV = Sales KDV - Return KDV
        report.SalesKdv.Should().Be(TotalSalesKdv, "50 sales totaling 100,000 TL at %18 = 18,000 TL KDV");
        report.ReturnKdvAdjustment.Should().Be(TotalReturnsKdv, "5 returns totaling 5,000 TL at %18 = 900 TL refund");
        report.NetOutputKdv.Should().Be(17_100m, "18,000 - 900 = 17,100 TL net output KDV");

        // Verify with KdvCalculationService for a single representative sale
        var kdvService = new KdvCalculationService();
        var sampleSale = kdvService.Calculate(new KdvCalculationInput(
            GrossSaleAmount: 2_000m,          // average sale = 100,000 / 50 = 2,000 TL
            KdvRate: KdvRate,
            CommissionRate: 0.15m,             // Trendyol rate
            WithholdingRate: 0m));
        sampleSale.KdvAmount.Should().Be(360m, "2,000 * 0.18 = 360 TL KDV per average sale");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 2: KdvDraft_InputVat_DeductedCorrectly
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_InputVat_DeductedCorrectly()
    {
        // Arrange
        var report = GenerateKdvDraftReport();

        // Act — compute expected commission KDV
        var totalCommission = PlatformCommissions.Sum(p => Math.Round(p.SalesAmount * p.CommissionRate, 2));
        var expectedCommissionKdv = Math.Round(totalCommission * KdvRate, 2);

        // Assert — Input KDV = Purchase KDV + Commission KDV
        report.PurchaseKdv.Should().Be(TotalPurchasesKdv, "20 purchases at 40,000 TL * %18 = 7,200 TL");
        report.CommissionKdv.Should().Be(expectedCommissionKdv,
            $"total commission {totalCommission} TL * %18 = {expectedCommissionKdv} TL");
        report.NetInputKdv.Should().Be(TotalPurchasesKdv + expectedCommissionKdv,
            "input KDV = purchase KDV + commission KDV");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 3: KdvDraft_ReturnAdjustment_Applied
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_ReturnAdjustment_Applied()
    {
        // Arrange
        var report = GenerateKdvDraftReport();

        // Simulate individual return transactions using TaxRecord entity
        var tenantId = Guid.NewGuid();
        var returnRecords = Enumerable.Range(1, ReturnCount)
            .Select(i => TaxRecord.Create(
                tenantId,
                period: Period,
                taxType: "KDV-Iade",
                taxableAmount: TotalReturnsNet / ReturnCount,     // 1,000 TL each
                taxAmount: TotalReturnsKdv / ReturnCount,          // 180 TL each
                dueDate: new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc)))
            .ToList();

        // Assert — return records match aggregate
        returnRecords.Should().HaveCount(ReturnCount);
        returnRecords.Sum(r => r.TaxAmount).Should().Be(TotalReturnsKdv,
            "5 returns * 180 TL KDV = 900 TL total return KDV");

        // Verify adjustment reduces output KDV
        var outputWithoutReturns = TotalSalesKdv;
        var outputWithReturns = report.NetOutputKdv;
        (outputWithoutReturns - outputWithReturns).Should().Be(TotalReturnsKdv,
            "return adjustment must exactly equal 900 TL");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 4: KdvDraft_NetPayable_Positive (output > input)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_NetPayable_Positive_WhenOutputExceedsInput()
    {
        // Arrange
        var report = GenerateKdvDraftReport();

        // Assert — payable KDV = net output - net input, must be > 0
        report.PayableKdv.Should().BeGreaterThan(0m,
            "output KDV (17,100) should exceed input KDV (~9,954) → positive payable");

        // Verify formula
        var expectedPayable = report.NetOutputKdv - report.NetInputKdv;
        report.PayableKdv.Should().Be(expectedPayable, "payable = net output - net input");

        // Cross-check: rough magnitude
        // Output: 17,100 TL, Input: 7,200 + 2,754 = 9,954 TL → Payable: ~7,146 TL
        report.PayableKdv.Should().BeInRange(7_000m, 7_500m,
            "payable should be approximately 7,146 TL");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 5: KdvDraft_WithCommissionVat_IncludedInDeductions
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_WithCommissionVat_IncludedInDeductions()
    {
        // Arrange — calculate commission KDV using domain entities
        var tenantId = Guid.NewGuid();
        var commissionRecords = new List<CommissionRecord>();

        foreach (var pc in PlatformCommissions)
        {
            var commissionAmount = Math.Round(pc.SalesAmount * pc.CommissionRate, 2);
            commissionRecords.Add(CommissionRecord.Create(
                tenantId,
                platform: pc.Platform,
                grossAmount: pc.SalesAmount,
                commissionRate: pc.CommissionRate,
                commissionAmount: commissionAmount,
                serviceFee: 0m,
                category: "Elektronik",
                commissionType: CommissionType.Percentage,
                rateSource: "StaticFallback"));
        }

        // Act — compute commission KDV
        var totalCommissionAmount = commissionRecords.Sum(r => r.CommissionAmount);
        var commissionKdv = Math.Round(totalCommissionAmount * KdvRate, 2);

        // Verify individual platform commissions
        var trendyolRec = commissionRecords.First(r => r.Platform == "Trendyol");
        var hbRec = commissionRecords.First(r => r.Platform == "Hepsiburada");
        var n11Rec = commissionRecords.First(r => r.Platform == "N11");

        // Assert — commission amounts
        trendyolRec.CommissionAmount.Should().Be(9_000m, "60,000 * 0.15 = 9,000");
        hbRec.CommissionAmount.Should().Be(4_500m, "25,000 * 0.18 = 4,500");
        n11Rec.CommissionAmount.Should().Be(1_800m, "15,000 * 0.12 = 1,800");
        totalCommissionAmount.Should().Be(15_300m, "9,000 + 4,500 + 1,800 = 15,300");

        // Assert — commission KDV
        commissionKdv.Should().Be(2_754m, "15,300 * 0.18 = 2,754 TL commission KDV");

        // Verify it's included in deductions
        var report = GenerateKdvDraftReport();
        report.CommissionKdv.Should().Be(commissionKdv);
        report.NetInputKdv.Should().BeGreaterThan(report.PurchaseKdv,
            "input KDV should include commission KDV beyond purchase KDV");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 6: KdvDraft_ReportText_ContainsAllLines
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_ReportText_ContainsAllLines()
    {
        // Arrange
        var report = GenerateKdvDraftReport();

        // Assert — report text contains all required sections
        report.ReportText.Should().Contain("KDV DRAFT REPORT", "header must be present");
        report.ReportText.Should().Contain("Mart 2026", "period must be identified");
        report.ReportText.Should().Contain("18.000,00", "sales KDV amount (tr-TR format)");
        report.ReportText.Should().Contain("-900,00", "return adjustment (negative, tr-TR format)");
        report.ReportText.Should().Contain("17.100,00", "net output KDV (tr-TR format)");
        report.ReportText.Should().Contain("7.200,00", "purchase KDV (tr-TR format)");
        report.ReportText.Should().Contain("2.754,00", "commission KDV (tr-TR format)");

        // Verify payable line exists
        report.ReportText.Should().Contain("ODENECEK KDV", "payable line must exist");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 7: KdvDraft_TaxRecord_EntityCreation_ForFilingPurposes
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void KdvDraft_TaxRecord_EntityCreation_ForFilingPurposes()
    {
        // Arrange
        var report = GenerateKdvDraftReport();
        var tenantId = Guid.NewGuid();
        var dueDate = new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc); // KDV beyanname tarihi

        // Act — create TaxRecord for the payable amount
        var taxRecord = TaxRecord.Create(
            tenantId,
            period: Period,
            taxType: "KDV",
            taxableAmount: TotalSalesNet - TotalReturnsNet, // net taxable base
            taxAmount: report.PayableKdv,
            dueDate: dueDate);

        // Assert — record correctly represents the filing
        taxRecord.Period.Should().Be("2026-03");
        taxRecord.TaxType.Should().Be("KDV");
        taxRecord.TaxableAmount.Should().Be(95_000m, "100,000 - 5,000 = 95,000 net taxable amount");
        taxRecord.TaxAmount.Should().Be(report.PayableKdv, "tax amount = payable KDV from draft");
        taxRecord.IsPaid.Should().BeFalse("draft report → not yet paid");

        // Mark as paid and verify
        taxRecord.MarkAsPaid();
        taxRecord.IsPaid.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════
    //  Report generation helper
    // ════════════════════════════════════════════════════════════

    private static KdvDraftReport GenerateKdvDraftReport()
    {
        // Output KDV
        var salesKdv = TotalSalesKdv;
        var returnKdvAdjustment = TotalReturnsKdv;
        var netOutputKdv = salesKdv - returnKdvAdjustment;

        // Commission KDV
        var totalCommission = PlatformCommissions.Sum(p => Math.Round(p.SalesAmount * p.CommissionRate, 2));
        var commissionKdv = Math.Round(totalCommission * KdvRate, 2);

        // Input KDV
        var purchaseKdv = TotalPurchasesKdv;
        var netInputKdv = purchaseKdv + commissionKdv;

        // Payable
        var payableKdv = netOutputKdv - netInputKdv;

        // Report text
        var reportText = BuildReportText(
            salesKdv, returnKdvAdjustment, netOutputKdv,
            purchaseKdv, commissionKdv, netInputKdv,
            payableKdv);

        return new KdvDraftReport(
            Period,
            salesKdv, returnKdvAdjustment, netOutputKdv,
            purchaseKdv, commissionKdv, netInputKdv,
            payableKdv,
            reportText);
    }

    private static string BuildReportText(
        decimal salesKdv, decimal returnKdvAdj, decimal netOutputKdv,
        decimal purchaseKdv, decimal commissionKdv, decimal netInputKdv,
        decimal payableKdv)
    {
        var tr = new CultureInfo("tr-TR");
        var sb = new StringBuilder();

        sb.AppendLine("KDV DRAFT REPORT - Mart 2026");
        sb.AppendLine(new string('\u2500', 35)); // ─ line

        sb.AppendLine(tr, $"Satis KDV (Hesaplanan):     {salesKdv,12:N2} TL");
        sb.AppendLine(tr, $"Iade KDV Duzeltme:          {(-returnKdvAdj),12:N2} TL");
        sb.AppendLine(tr, $"Net Satis KDV:              {netOutputKdv,12:N2} TL");
        sb.AppendLine();

        sb.AppendLine(tr, $"Alis KDV (Indirilecek):     {purchaseKdv,12:N2} TL");
        sb.AppendLine(tr, $"Komisyon KDV:               {commissionKdv,12:N2} TL");
        sb.AppendLine(tr, $"Net Indirilecek KDV:        {netInputKdv,12:N2} TL");
        sb.AppendLine();

        sb.AppendLine(tr, $"ODENECEK KDV:               {payableKdv,12:N2} TL");

        return sb.ToString();
    }
}
