using FluentAssertions;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "EInvoice")]
public class EInvoiceScenarioMatrixTests
{
    // ═══════════════════════════════════════════════════════════
    // ProfileID × InvoiceTypeCode full matrix (3 × 6 = 18)
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS)]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.IADE)]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT)]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.ISTISNA)]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.OZELMATRAH)]
    [InlineData(EInvoiceScenario.TEMELFATURA, EInvoiceType.IHRACKAYITLI)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.SATIS)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.IADE)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.TEVKIFAT)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.ISTISNA)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.OZELMATRAH)]
    [InlineData(EInvoiceScenario.TICARIFATURA, EInvoiceType.IHRACKAYITLI)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.SATIS)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.IADE)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.TEVKIFAT)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.ISTISNA)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.OZELMATRAH)]
    [InlineData(EInvoiceScenario.EARSIVFATURA, EInvoiceType.IHRACKAYITLI)]
    public void Create_AllScenarioTypes_ShouldSucceed(EInvoiceScenario scenario, EInvoiceType type)
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        // Act
        var doc = EInvoiceDocument.Create(
            gibUuid: uuid, ettnNo: "MTX2026000000001",
            scenario: scenario, type: type,
            issueDate: DateTime.UtcNow, sellerVkn: "1234567890",
            sellerTitle: "MesTech A.S.", buyerTitle: "Alici Ltd.",
            providerId: "Sovos", createdBy: "dev5");

        // Assert
        doc.Should().NotBeNull();
        doc.Scenario.Should().Be(scenario);
        doc.Type.Should().Be(type);
        doc.Status.Should().Be(EInvoiceStatus.Draft);
        doc.GibUuid.Should().Be(uuid);
        doc.DomainEvents.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════════════════
    // TEVKIFAT withholding rate calculations
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0.2, 1000, 200)]    // 2/10 tevkifat
    [InlineData(0.5, 1000, 500)]    // 5/10 tevkifat
    [InlineData(0.9, 1000, 900)]    // 9/10 tevkifat
    public void Create_TEVKIFAT_WithWithholding_ShouldCalculateCorrectly(
        decimal rate, decimal taxAmount, decimal expectedWithholding)
    {
        // Arrange
        var doc = CreateDoc(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(
            lineExtension: 5000m, taxExclusive: 5000m,
            taxInclusive: 5000m + taxAmount, allowance: 0m,
            taxAmount: taxAmount, payable: 5000m + taxAmount);

        // Act
        doc.SetWithholding(rate);

        // Assert
        doc.WithholdingRate.Should().Be(rate);
        doc.WithholdingAmount.Should().Be(expectedWithholding);
        doc.NetPayable.Should().Be(doc.PayableAmount - expectedWithholding);
    }

    // ═══════════════════════════════════════════════════════════
    // EARSIVFATURA without BuyerEmail — domain doesn't enforce
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Create_EARSIVFATURA_WithoutBuyerEmail_ShouldStillCreate()
    {
        // Arrange & Act
        var doc = EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(), ettnNo: "EAR2026000000001",
            scenario: EInvoiceScenario.EARSIVFATURA, type: EInvoiceType.SATIS,
            issueDate: DateTime.UtcNow, sellerVkn: "1234567890",
            sellerTitle: "MesTech A.S.", buyerTitle: "Bireysel Alici",
            providerId: "Sovos", createdBy: "dev5");

        // Assert — BuyerEmail is null, domain allows it (validator's responsibility)
        doc.Should().NotBeNull();
        doc.Scenario.Should().Be(EInvoiceScenario.EARSIVFATURA);
        doc.BuyerEmail.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // Multiple KDV rates accumulation (%1, %10, %20)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Create_MultipleKdvRates_ShouldAccumulateCorrectly()
    {
        // Arrange — simulate 3 lines with different KDV rates
        // Line 1: 1000 TL @ %1  = 10 TL KDV
        // Line 2: 2000 TL @ %10 = 200 TL KDV
        // Line 3: 3000 TL @ %20 = 600 TL KDV
        // Total: lineExtension=6000, taxAmount=810, payable=6810
        var doc = CreateDoc(EInvoiceScenario.TICARIFATURA, EInvoiceType.SATIS);

        var totalLineExtension = 1000m + 2000m + 3000m;  // 6000
        var totalTax = 10m + 200m + 600m;                 // 810
        var totalPayable = totalLineExtension + totalTax;  // 6810

        // Act
        doc.SetFinancials(
            lineExtension: totalLineExtension,
            taxExclusive: totalLineExtension,
            taxInclusive: totalPayable,
            allowance: 0m,
            taxAmount: totalTax,
            payable: totalPayable);

        // Assert
        doc.LineExtensionAmount.Should().Be(6000m);
        doc.TaxAmount.Should().Be(810m);
        doc.PayableAmount.Should().Be(6810m);
        doc.TaxInclusiveAmount.Should().Be(6810m);
        doc.CurrencyCode.Should().Be("TRY");
    }

    // ═══════════════════════════════════════════════════════════
    // SetWithholding boundary validation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetWithholding_NegativeRate_ShouldThrow()
    {
        // Arrange
        var doc = CreateDoc(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(1000m, 1000m, 1180m, 0m, 180m, 1180m);

        // Act
        var act = () => doc.SetWithholding(-0.1m);

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void SetWithholding_RateAboveOne_ShouldThrow()
    {
        // Arrange
        var doc = CreateDoc(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(1000m, 1000m, 1180m, 0m, 180m, 1180m);

        // Act
        var act = () => doc.SetWithholding(1.01m);

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SetWithholding_BoundaryRates_ShouldSucceed(decimal rate)
    {
        // Arrange
        var doc = CreateDoc(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(1000m, 1000m, 1180m, 0m, 180m, 1180m);

        // Act
        doc.SetWithholding(rate);

        // Assert
        doc.WithholdingRate.Should().Be(rate);
    }

    // ═══════════════════════════════════════════════════════════
    // Financial rounding — 2 decimal places (kurus precision)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Financial_Rounding_TwoDecimalPlaces()
    {
        // Arrange — edge case: TaxAmount that produces 0.005 kurus rounding
        // TaxAmount = 33.33, rate = 0.15 → 33.33 * 0.15 = 4.9995 → rounded to 5.00
        var doc = CreateDoc(EInvoiceScenario.TEMELFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(
            lineExtension: 185.17m, taxExclusive: 185.17m,
            taxInclusive: 218.50m, allowance: 0m,
            taxAmount: 33.33m, payable: 218.50m);

        // Act
        doc.SetWithholding(0.15m);

        // Assert — Math.Round(33.33 * 0.15, 2) = Math.Round(4.9995, 2) = 5.00
        doc.WithholdingAmount.Should().Be(5.00m);
        doc.NetPayable.Should().Be(218.50m - 5.00m);
    }

    [Fact]
    public void Financial_Rounding_HalfKurus_MidpointRounding()
    {
        // Arrange — TaxAmount = 100.01, rate = 0.5 → 50.005 → banker's rounding = 50.00
        var doc = CreateDoc(EInvoiceScenario.TICARIFATURA, EInvoiceType.TEVKIFAT);
        doc.SetFinancials(
            lineExtension: 500m, taxExclusive: 500m,
            taxInclusive: 600.01m, allowance: 0m,
            taxAmount: 100.01m, payable: 600.01m);

        // Act
        doc.SetWithholding(0.5m);

        // Assert — Math.Round(100.01 * 0.5, 2) = Math.Round(50.005, 2) = 50.00 (banker's)
        doc.WithholdingAmount.Should().Be(Math.Round(100.01m * 0.5m, 2));
    }

    // ═══════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════

    private static EInvoiceDocument CreateDoc(EInvoiceScenario scenario, EInvoiceType type) =>
        EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(), ettnNo: "MTX2026000000001",
            scenario: scenario, type: type,
            issueDate: DateTime.UtcNow, sellerVkn: "1234567890",
            sellerTitle: "MesTech A.S.", buyerTitle: "Test Alici Ltd.",
            providerId: "Sovos", createdBy: "dev5");
}
