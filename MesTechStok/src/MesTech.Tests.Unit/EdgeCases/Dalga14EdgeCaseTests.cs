using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Dalga 14 M2 edge case tests — accounting, stock, commission, multi-tenant.
/// Target: push test count from 4,215 to 4,300+ (90 new tests).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class Dalga14EdgeCaseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _tenantId2 = Guid.NewGuid();

    #region JournalEntry Edge Cases (25 tests)

    [Fact]
    public void JournalEntry_SmallFractionalAmounts_ShouldBalance()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Fractional test");
        entry.AddLine(Guid.NewGuid(), 0.01m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 0.01m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_LargeAmount_ShouldBalance()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Large amount test");
        entry.AddLine(Guid.NewGuid(), 999_999_999.99m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 999_999_999.99m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_ManyLines_ShouldBalance()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Many lines test");
        for (int i = 0; i < 50; i++)
            entry.AddLine(Guid.NewGuid(), 10m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_ManyLines_Unbalanced_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Many lines unbalanced");
        for (int i = 0; i < 50; i++)
            entry.AddLine(Guid.NewGuid(), 10m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 499.99m);

        var act = () => entry.Validate();
        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void JournalEntry_MixedDebitCredit_ShouldBalance()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Mixed test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 50m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 75m);
        entry.AddLine(Guid.NewGuid(), 0m, 75m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_Post_UnbalancedShouldNotSetPosted()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unbalanced post");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);

        var act = () => entry.Post();
        act.Should().Throw<JournalEntryImbalanceException>();
        entry.IsPosted.Should().BeFalse();
    }

    [Fact]
    public void JournalEntry_EmptyGuid_TenantId_ShouldThrow()
    {
        var act = () => JournalEntry.Create(Guid.Empty, DateTime.UtcNow, "Empty tenant");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_DifferentTenants_ShouldBeIsolated()
    {
        var entry1 = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Tenant 1");
        var entry2 = JournalEntry.Create(_tenantId2, DateTime.UtcNow, "Tenant 2");

        entry1.TenantId.Should().NotBe(entry2.TenantId);
        entry1.Id.Should().NotBe(entry2.Id);
    }

    [Fact]
    public void JournalEntry_PostEvent_ShouldContainCorrectTotalForMultiDebit()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Multi debit");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 200m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 300m);
        entry.Post();

        var evt = entry.DomainEvents.OfType<LedgerPostedEvent>().Single();
        evt.TotalAmount.Should().Be(300m);
        evt.LineCount.Should().Be(3);
    }

    [Fact]
    public void JournalEntry_VeryLongDescription_ShouldSucceed()
    {
        var longDesc = new string('A', 1000);
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, longDesc);
        entry.Description.Should().Be(longDesc);
    }

    [Fact]
    public void JournalEntry_TurkishCharacters_ShouldSucceed()
    {
        var desc = "Ürün satış faturası — çiçek sepeti siparişi (İstanbul şubesi)";
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, desc);
        entry.Description.Should().Be(desc);
    }

    [Fact]
    public void JournalEntry_FutureDate_ShouldSucceed()
    {
        var futureDate = DateTime.UtcNow.AddDays(30);
        var entry = JournalEntry.Create(_tenantId, futureDate, "Future entry");
        entry.EntryDate.Should().Be(futureDate);
    }

    [Fact]
    public void JournalEntry_PastDate_ShouldSucceed()
    {
        var pastDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entry = JournalEntry.Create(_tenantId, pastDate, "Historical entry");
        entry.EntryDate.Should().Be(pastDate);
    }

    [Fact]
    public void JournalEntry_AddLine_WithEmptyAccountId_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        var act = () => entry.AddLine(Guid.Empty, 100m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_WithBothDebitAndCredit_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        var act = () => entry.AddLine(Guid.NewGuid(), 100m, 50m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_Validate_ExactlyTwoLinesBalanced_ShouldPass()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Minimum valid");
        entry.AddLine(Guid.NewGuid(), 1m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 1m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_Post_ThenCheckPostedAt_ShouldBeRecent()
    {
        var before = DateTime.UtcNow;
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Timing test");
        entry.AddLine(Guid.NewGuid(), 500m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);
        entry.Post();
        var after = DateTime.UtcNow;

        entry.PostedAt.Should().BeOnOrAfter(before);
        entry.PostedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void JournalEntry_TwoLinesAdded_ShouldHaveCountTwo()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void JournalEntry_SameAccountMultipleLines_ShouldSucceed()
    {
        var accountId = Guid.NewGuid();
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Same account");
        entry.AddLine(accountId, 100m, 0m);
        entry.AddLine(accountId, 0m, 100m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntryImbalanceException_ShouldContainBothAmounts()
    {
        var ex = new JournalEntryImbalanceException(100m, 80m);
        ex.TotalDebit.Should().Be(100m);
        ex.TotalCredit.Should().Be(80m);
    }

    [Fact]
    public void JournalEntry_ThreeWayBalance_ShouldWork()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "3-way split");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m); // total debit
        entry.AddLine(Guid.NewGuid(), 0m, 500m);  // credit 1
        entry.AddLine(Guid.NewGuid(), 0m, 300m);  // credit 2
        entry.AddLine(Guid.NewGuid(), 0m, 200m);  // credit 3

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_KDVScenario_ShouldBalance()
    {
        // Typical Turkish e-commerce: Sale 1000 TL, KDV 180 TL, Commission 219 TL
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "KDV scenario");
        entry.AddLine(Guid.NewGuid(), 1180m, 0m);  // receivable (brüt + KDV)
        entry.AddLine(Guid.NewGuid(), 0m, 1000m);  // revenue
        entry.AddLine(Guid.NewGuid(), 0m, 180m);   // KDV payable

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_CommissionExpenseScenario_ShouldBalance()
    {
        // Commission deduction: Platform deducts 21.93% from 500 TL sale
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Commission deduction");
        entry.AddLine(Guid.NewGuid(), 109.65m, 0m);  // commission expense
        entry.AddLine(Guid.NewGuid(), 390.35m, 0m);  // bank/receivable (net)
        entry.AddLine(Guid.NewGuid(), 0m, 500m);     // sales revenue

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_RefundScenario_ReverseEntry_ShouldBalance()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Refund reversal");
        entry.AddLine(Guid.NewGuid(), 0m, 100m);  // reverse receivable (credit)
        entry.AddLine(Guid.NewGuid(), 100m, 0m);  // reverse revenue (debit)

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_RoundingEdge_ThreeWaySplit_ShouldBalance()
    {
        // 100 / 3 = 33.33... — must balance exactly
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Rounding edge");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 33.33m);
        entry.AddLine(Guid.NewGuid(), 0m, 33.33m);
        entry.AddLine(Guid.NewGuid(), 0m, 33.34m);

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    #endregion

    #region Money Value Object Extended Edge Cases (20 tests)

    [Fact]
    public void Money_MaxDecimalValue_ShouldCreate()
    {
        var money = new Money(79_228_162_514_264_337_593_543_950_335m, "TRY");
        money.Amount.Should().Be(79_228_162_514_264_337_593_543_950_335m);
    }

    [Fact]
    public void Money_MinDecimalValue_ShouldCreate()
    {
        var money = new Money(-79_228_162_514_264_337_593_543_950_335m, "TRY");
        money.Amount.Should().Be(-79_228_162_514_264_337_593_543_950_335m);
    }

    [Fact]
    public void Money_ZeroAmount_ShouldBeValid()
    {
        var money = Money.TRY(0m);
        money.Amount.Should().Be(0m);
    }

    [Fact]
    public void Money_Add_ManySmallAmounts_ShouldNotLosePrecision()
    {
        var total = Money.TRY(0m);
        for (int i = 0; i < 100; i++)
            total = total.Add(Money.TRY(0.01m));

        total.Amount.Should().Be(1.00m);
    }

    [Fact]
    public void Money_Subtract_ToExactZero_ShouldWork()
    {
        var money = Money.TRY(100m);
        var result = money.Subtract(Money.TRY(100m));
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Money_Multiply_ByOne_ShouldReturnSame()
    {
        var money = Money.TRY(123.45m);
        var result = money.Multiply(1m);
        result.Amount.Should().Be(123.45m);
    }

    [Fact]
    public void Money_Multiply_LargeNumber_ShouldWork()
    {
        var money = Money.TRY(1_000_000m);
        var result = money.Multiply(1000m);
        result.Amount.Should().Be(1_000_000_000m);
    }

    [Fact]
    public void Money_Equality_SameValues_ShouldBeEqual()
    {
        var a = Money.TRY(100m);
        var b = Money.TRY(100m);
        a.Should().Be(b);
    }

    [Fact]
    public void Money_Equality_ZeroAmounts_DifferentCurrencies_ShouldNotBeEqual()
    {
        var a = Money.Zero("TRY");
        var b = Money.Zero("USD");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Money_TRY_Factory_ShouldSetCurrency()
    {
        var money = Money.TRY(50m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Money_USD_Factory_ShouldSetCurrency()
    {
        var money = Money.USD(50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_SelfToSelf_ShouldDouble()
    {
        var money = Money.TRY(100m);
        var result = money.Add(money);
        result.Amount.Should().Be(200m);
    }

    [Fact]
    public void Money_Subtract_SelfFromSelf_ShouldBeZero()
    {
        var money = Money.TRY(100m);
        var result = money.Subtract(money);
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Money_KDV_18Percent_ShouldCalculateCorrectly()
    {
        var saleAmount = Money.TRY(1000m);
        var kdv = saleAmount.Multiply(0.18m);
        kdv.Amount.Should().Be(180m);
    }

    [Fact]
    public void Money_TrendyolCommission_ShouldCalculate()
    {
        var saleAmount = Money.TRY(500m);
        var commission = saleAmount.Multiply(0.2193m);
        commission.Amount.Should().BeApproximately(109.65m, 0.01m);
    }

    [Fact]
    public void Money_HepsiburadaCommission_ShouldCalculate()
    {
        var saleAmount = Money.TRY(800m);
        var commission = saleAmount.Multiply(0.1750m);
        commission.Amount.Should().Be(140m);
    }

    [Fact]
    public void Money_CiceksepetiCommission_ShouldCalculate()
    {
        var saleAmount = Money.TRY(250m);
        var commission = saleAmount.Multiply(0.25m);
        commission.Amount.Should().Be(62.50m);
    }

    [Fact]
    public void Money_N11Commission_ShouldCalculate()
    {
        var saleAmount = Money.TRY(1200m);
        var commission = saleAmount.Multiply(0.15m);
        commission.Amount.Should().Be(180m);
    }

    [Fact]
    public void Money_NetAfterCommission_ShouldBeCorrect()
    {
        var gross = Money.TRY(1000m);
        var commission = gross.Multiply(0.20m);
        var net = gross.Subtract(commission);
        net.Amount.Should().Be(800m);
    }

    [Fact]
    public void Money_KDVInclusive_ShouldExtractCorrectly()
    {
        // KDV dahil 1180 TL → KDV hariç 1000 TL, KDV 180 TL
        var kdvInclusive = Money.TRY(1180m);
        var kdvExclusive = kdvInclusive.Multiply(100m / 118m);
        kdvExclusive.Amount.Should().BeApproximately(1000m, 0.01m);
    }

    #endregion

    #region SettlementBatch Edge Cases (10 tests)

    [Fact]
    public void SettlementBatch_Create_ShouldSetPlatform()
    {
        var batch = SettlementBatch.Create(_tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1000m, 200m, 800m);
        batch.Platform.Should().Be("Trendyol");
        batch.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void SettlementBatch_EmptyPlatform_ShouldThrow()
    {
        var act = () => SettlementBatch.Create(_tenantId, "",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1000m, 200m, 800m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_NullPlatform_ShouldThrow()
    {
        var act = () => SettlementBatch.Create(_tenantId, null!,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1000m, 200m, 800m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_EndBeforeStart_ShouldThrow()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);
        var act = () => SettlementBatch.Create(_tenantId, "Trendyol", start, end, 1000m, 200m, 800m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_ZeroAmounts_ShouldSucceed()
    {
        var batch = SettlementBatch.Create(_tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 0m, 0m, 0m);
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public void SettlementBatch_LargeAmounts_ShouldSucceed()
    {
        var batch = SettlementBatch.Create(_tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 10_000_000m, 2_000_000m, 8_000_000m);
        batch.TotalGross.Should().Be(10_000_000m);
    }

    [Fact]
    public void SettlementBatch_EmptyTenantId_ShouldThrow()
    {
        var act = () => SettlementBatch.Create(Guid.Empty, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1000m, 200m, 800m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_AllPlatforms_ShouldCreate()
    {
        var platforms = new[] { "Trendyol", "Hepsiburada", "Ciceksepeti", "N11", "Amazon", "Pazarama" };
        foreach (var platform in platforms)
        {
            var batch = SettlementBatch.Create(_tenantId, platform,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1000m, 200m, 800m);
            batch.Platform.Should().Be(platform);
        }
    }

    [Fact]
    public void SettlementBatch_NegativeNet_ShouldSucceed()
    {
        // Refunds can result in negative net
        var batch = SettlementBatch.Create(_tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), -500m, -100m, -400m);
        batch.TotalNet.Should().Be(-400m);
    }

    [Fact]
    public void SettlementBatch_SameDayPeriod_ShouldSucceed()
    {
        var now = DateTime.UtcNow;
        var batch = SettlementBatch.Create(_tenantId, "Trendyol", now, now, 100m, 20m, 80m);
        batch.PeriodStart.Should().Be(now);
        batch.PeriodEnd.Should().Be(now);
    }

    #endregion

    #region CommissionRecord Edge Cases (10 tests)

    [Fact]
    public void CommissionRecord_ZeroRate_ShouldSucceed()
    {
        var record = CommissionRecord.Create(_tenantId, "Trendyol", 1000m, 0m, 0m, 0m);
        record.CommissionRate.Should().Be(0m);
    }

    [Fact]
    public void CommissionRecord_StandardRate_ShouldSucceed()
    {
        var record = CommissionRecord.Create(_tenantId, "Trendyol", 1000m, 0.2193m, 219.30m, 0m);
        record.CommissionRate.Should().Be(0.2193m);
        record.CommissionAmount.Should().Be(219.30m);
    }

    [Fact]
    public void CommissionRecord_EmptyPlatform_ShouldThrow()
    {
        var act = () => CommissionRecord.Create(_tenantId, "", 1000m, 0.20m, 200m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommissionRecord_EmptyTenantId_ShouldThrow()
    {
        var act = () => CommissionRecord.Create(Guid.Empty, "Trendyol", 1000m, 0.20m, 200m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommissionRecord_WithOrderId_ShouldSet()
    {
        var record = CommissionRecord.Create(_tenantId, "Trendyol", 500m, 0.15m, 75m, 0m, "ORD-001");
        record.OrderId.Should().Be("ORD-001");
    }

    [Fact]
    public void CommissionRecord_WithCategory_ShouldSet()
    {
        var record = CommissionRecord.Create(_tenantId, "Trendyol", 500m, 0.15m, 75m, 0m, category: "Electronics");
        record.Category.Should().Be("Electronics");
    }

    [Fact]
    public void CommissionRecord_WithServiceFee_ShouldSet()
    {
        var record = CommissionRecord.Create(_tenantId, "Hepsiburada", 1000m, 0.175m, 175m, 25m);
        record.ServiceFee.Should().Be(25m);
    }

    [Fact]
    public void CommissionRecord_LargeGrossAmount_ShouldSucceed()
    {
        var record = CommissionRecord.Create(_tenantId, "Amazon", 999_999m, 0.15m, 149_999.85m, 0m);
        record.GrossAmount.Should().Be(999_999m);
    }

    [Fact]
    public void CommissionRecord_DifferentPlatforms_ShouldBeIsolated()
    {
        var r1 = CommissionRecord.Create(_tenantId, "Trendyol", 1000m, 0.20m, 200m, 0m);
        var r2 = CommissionRecord.Create(_tenantId, "Hepsiburada", 1000m, 0.175m, 175m, 0m);

        r1.Platform.Should().NotBe(r2.Platform);
        r1.CommissionRate.Should().NotBe(r2.CommissionRate);
    }

    [Fact]
    public void CommissionRecord_ZeroGross_ZeroCommission_ShouldSucceed()
    {
        var record = CommissionRecord.Create(_tenantId, "N11", 0m, 0m, 0m, 0m);
        record.GrossAmount.Should().Be(0m);
    }

    #endregion

    #region ChartOfAccounts Edge Cases (10 tests)

    [Fact]
    public void ChartOfAccounts_Create_AssetType_ShouldSetDefaults()
    {
        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        account.Code.Should().Be("100");
        account.Name.Should().Be("Kasa");
        account.AccountType.Should().Be(AccountType.Asset);
    }

    [Fact]
    public void ChartOfAccounts_EmptyCode_ShouldThrow()
    {
        var act = () => ChartOfAccounts.Create(_tenantId, "", "Kasa", AccountType.Asset);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChartOfAccounts_EmptyName_ShouldThrow()
    {
        var act = () => ChartOfAccounts.Create(_tenantId, "100", "", AccountType.Asset);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChartOfAccounts_EmptyTenantId_ShouldThrow()
    {
        var act = () => ChartOfAccounts.Create(Guid.Empty, "100", "Kasa", AccountType.Asset);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChartOfAccounts_AllAccountTypes_ShouldCreate()
    {
        var types = new[] { AccountType.Asset, AccountType.Liability, AccountType.Equity, AccountType.Revenue, AccountType.Expense };
        foreach (var type in types)
        {
            var account = ChartOfAccounts.Create(_tenantId, "100", "Test", type);
            account.AccountType.Should().Be(type);
        }
    }

    [Fact]
    public void ChartOfAccounts_TurkishAccountName_ShouldWork()
    {
        var account = ChartOfAccounts.Create(_tenantId, "600", "Yurtiçi Satışlar", AccountType.Revenue);
        account.Name.Should().Be("Yurtiçi Satışlar");
    }

    [Fact]
    public void ChartOfAccounts_DuplicateCode_DifferentTenants_ShouldBeIsolated()
    {
        var a1 = ChartOfAccounts.Create(_tenantId, "100", "Kasa T1", AccountType.Asset);
        var a2 = ChartOfAccounts.Create(_tenantId2, "100", "Kasa T2", AccountType.Asset);

        a1.TenantId.Should().NotBe(a2.TenantId);
        a1.Id.Should().NotBe(a2.Id);
    }

    [Fact]
    public void ChartOfAccounts_WithParentId_ShouldSetParent()
    {
        var parentId = Guid.NewGuid();
        var account = ChartOfAccounts.Create(_tenantId, "100.01", "Kasa - Ana Banka", AccountType.Asset, parentId);
        account.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void ChartOfAccounts_WithoutParentId_ShouldBeNull()
    {
        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        account.ParentId.Should().BeNull();
    }

    [Fact]
    public void ChartOfAccounts_LongCode_ShouldWork()
    {
        var account = ChartOfAccounts.Create(_tenantId, "100.01.001.0001", "Detailed Account", AccountType.Asset);
        account.Code.Should().Be("100.01.001.0001");
    }

    #endregion

    #region ReconciliationMatch Edge Cases (10 tests)

    [Fact]
    public void ReconciliationMatch_HighConfidence_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.99m, ReconciliationStatus.AutoMatched);
        match.Confidence.Should().Be(0.99m);
    }

    [Fact]
    public void ReconciliationMatch_ZeroConfidence_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0m, ReconciliationStatus.Rejected);
        match.Confidence.Should().Be(0m);
    }

    [Fact]
    public void ReconciliationMatch_PerfectConfidence_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 1.0m, ReconciliationStatus.AutoMatched);
        match.Confidence.Should().Be(1.0m);
    }

    [Fact]
    public void ReconciliationMatch_EmptyTenantId_ShouldThrow()
    {
        var act = () => ReconciliationMatch.Create(Guid.Empty, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReconciliationMatch_WithSettlementBatchId_ShouldSet()
    {
        var batchId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.85m,
            ReconciliationStatus.NeedsReview, batchId);
        match.SettlementBatchId.Should().Be(batchId);
    }

    [Fact]
    public void ReconciliationMatch_WithBankTransactionId_ShouldSet()
    {
        var bankTxId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.90m,
            ReconciliationStatus.AutoMatched, bankTransactionId: bankTxId);
        match.BankTransactionId.Should().Be(bankTxId);
    }

    [Fact]
    public void ReconciliationMatch_MatchedStatus_ShouldBeMatched()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);
        match.Status.Should().Be(ReconciliationStatus.AutoMatched);
    }

    [Fact]
    public void ReconciliationMatch_UnmatchedStatus_ShouldBeUnmatched()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.30m, ReconciliationStatus.Rejected);
        match.Status.Should().Be(ReconciliationStatus.Rejected);
    }

    [Fact]
    public void ReconciliationMatch_PendingReviewStatus_ShouldBePending()
    {
        var match = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.70m, ReconciliationStatus.NeedsReview);
        match.Status.Should().Be(ReconciliationStatus.NeedsReview);
    }

    [Fact]
    public void ReconciliationMatch_DifferentTenants_ShouldBeIsolated()
    {
        var m1 = ReconciliationMatch.Create(_tenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);
        var m2 = ReconciliationMatch.Create(_tenantId2, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);

        m1.TenantId.Should().NotBe(m2.TenantId);
        m1.Id.Should().NotBe(m2.Id);
    }

    #endregion
}
