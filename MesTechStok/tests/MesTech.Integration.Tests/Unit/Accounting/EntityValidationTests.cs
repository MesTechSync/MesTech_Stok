using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// N2-KALITE — Group 3: Entity validation tests.
/// Tests domain entity validation rules for accounting entities:
/// SalaryRecord, FixedExpense, PenaltyRecord, CalendarEvent, Expense, Income, TaxRecord.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Accounting")]
[Trait("Group", "EntityValidation")]
public class EntityValidationTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════════════
    // 1. Expense — Negatif tutar seti (basit setter entity, no validation)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Expense_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var expense = new Expense
        {
            TenantId = _tenantId,
            Description = "Test gider",
            ExpenseType = ExpenseType.Diger,
            Date = DateTime.UtcNow
        };

        // Act & Assert — SetAmount validates amount > 0 at domain level
        var act = () => expense.SetAmount(-500m);
        act.Should().Throw<ArgumentException>(
            "domain entity SetAmount rejects negative amounts");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. Expense — Bos aciklama seti
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Expense_EmptyDescription_SetsEmptyString()
    {
        // Arrange & Act
        var expense = new Expense
        {
            TenantId = _tenantId,
            Description = "",
            ExpenseType = ExpenseType.Kira,
            Date = DateTime.UtcNow
        };
        expense.SetAmount(100m);

        // Assert — Setter bazli entity, bos string kabul eder
        expense.Description.Should().BeEmpty(
            "basit setter entity bos description'i kabul eder");
        expense.Amount.Should().Be(100m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. Income — Net tutar (commission hesaplama simülasyonu)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Income_NetAmount_CalculatesCorrectly()
    {
        // Arrange — 10.000 TL brut gelir, %12 komisyon
        var grossAmount = 10_000m;
        var commissionRate = 0.12m;
        var commission = Math.Round(grossAmount * commissionRate, 2);
        var netAmount = grossAmount - commission;

        // Act
        var income = new Income
        {
            TenantId = _tenantId,
            Description = "Trendyol satis geliri",
            Amount = netAmount,
            IncomeType = IncomeType.Satis,
            Date = DateTime.UtcNow
        };

        // Assert
        income.Amount.Should().Be(8_800m, "Net = 10.000 - 1.200 = 8.800 TL");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. Income — Negatif komisyon (entity validation yoklugu kontrolu)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Income_NegativeCommission_AmountStillSets()
    {
        // Arrange & Act — Negatif tutar atanabilir (setter entity)
        var income = new Income
        {
            TenantId = _tenantId,
            Description = "Hata senaryosu",
            Amount = -100m,
            IncomeType = IncomeType.Diger,
            Date = DateTime.UtcNow
        };

        // Assert — Setter bazli entity, negatif tutari kabul eder
        income.Amount.Should().BeNegative(
            "basit setter entity negatif tutari kabul eder — Application layer kontrol etmeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. TaxRecord — Gecersiz period/taxType → ArgumentException
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void TaxRecord_InvalidPeriodAndType_ThrowsValidation()
    {
        // Arrange & Act & Assert — Bos period
        var act1 = () => TaxRecord.Create(
            _tenantId, "", "KDV", 10_000m, 2_000m, DateTime.UtcNow);
        act1.Should().Throw<ArgumentException>("bos period kabul edilmemeli");

        // Bos taxType
        var act2 = () => TaxRecord.Create(
            _tenantId, "2026-03", "", 10_000m, 2_000m, DateTime.UtcNow);
        act2.Should().Throw<ArgumentException>("bos taxType kabul edilmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. SalaryRecord — Net maas hesaplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SalaryRecord_NetSalary_CalculatesCorrectly()
    {
        // Arrange — Brut 25.000 TL, SGK isci %14 = 3.500, GV %15 = 3.750, DV %0.759 = 189.75
        var gross = 25_000m;
        var sgkEmployee = 3_500m;   // %14
        var incomeTax = 3_750m;     // %15 (basitlestirmis)
        var stampTax = 189.75m;     // %0.759

        // Act
        var salary = SalaryRecord.Create(
            _tenantId, "Ahmet Yilmaz", gross,
            sgkEmployer: 5_500m,     // %22
            sgkEmployee: sgkEmployee,
            incomeTax: incomeTax,
            stampTax: stampTax,
            year: 2026, month: 3);

        // Assert — NetSalary = Gross - SGKEmployee - IncomeTax - StampTax
        var expectedNet = gross - sgkEmployee - incomeTax - stampTax;
        salary.NetSalary.Should().Be(expectedNet,
            "NetSalary = 25.000 - 3.500 - 3.750 - 189.75 = 17.560.25 TL");
        salary.NetSalary.Should().Be(17_560.25m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. SalaryRecord — TotalEmployerCost (Gross + SGK isveren)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SalaryRecord_TotalEmployerCost_IncludesSGK()
    {
        // Arrange
        var gross = 20_000m;
        var sgkEmployer = 4_400m; // %22

        // Act
        var salary = SalaryRecord.Create(
            _tenantId, "Mehmet Demir", gross,
            sgkEmployer: sgkEmployer,
            sgkEmployee: 2_800m,
            incomeTax: 3_000m,
            stampTax: 151.80m,
            year: 2026, month: 3);

        // Assert — TotalEmployerCost = GrossSalary + SGKEmployer
        salary.TotalEmployerCost.Should().Be(gross + sgkEmployer,
            "TotalEmployerCost = 20.000 + 4.400 = 24.400 TL");
        salary.TotalEmployerCost.Should().Be(24_400m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. CalendarEvent — Gecmis tarihli etkinlik olusturulabilir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void CalendarEvent_PastDate_Allowed()
    {
        // Arrange — 1 ay onceki tarih
        var pastStart = DateTime.UtcNow.AddMonths(-1);
        var pastEnd = pastStart.AddHours(2);

        // Act
        var ev = CalendarEvent.Create(_tenantId, "Gecmis Toplanti", pastStart, pastEnd,
            EventType.Meeting);

        // Assert — Gecmis tarihli etkinlik olusturulabilir
        ev.Should().NotBeNull();
        ev.StartAt.Should().BeBefore(DateTime.UtcNow);
        ev.Title.Should().Be("Gecmis Toplanti");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. FixedExpense — Sifir tutar → ArgumentOutOfRangeException
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void FixedExpense_ZeroAmount_ThrowsValidation()
    {
        // Arrange & Act & Assert
        var act = () => FixedExpense.Create(
            _tenantId, "Ofis Kirasi", 0m, dayOfMonth: 1,
            startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("monthlyAmount",
                "sifir tutar sabit gider icin gecersiz");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. PenaltyRecord — Gelecek tarihli ceza olusturulabilir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void PenaltyRecord_FuturePenaltyDate_Allowed()
    {
        // Arrange — 1 hafta sonraki tarih (henuz kesinlesmemis ceza)
        var futureDate = DateTime.UtcNow.AddDays(7);

        // Act
        var penalty = PenaltyRecord.Create(
            _tenantId,
            PenaltySource.Trendyol,
            "Gec kargo cezasi",
            250m,
            penaltyDate: futureDate,
            dueDate: futureDate.AddDays(30),
            referenceNumber: "PEN-2026-001");

        // Assert — Gelecek tarihli ceza olusturulabilir
        penalty.Should().NotBeNull();
        penalty.PenaltyDate.Should().BeAfter(DateTime.UtcNow);
        penalty.Amount.Should().Be(250m);
        penalty.Source.Should().Be(PenaltySource.Trendyol);
        penalty.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }
}
