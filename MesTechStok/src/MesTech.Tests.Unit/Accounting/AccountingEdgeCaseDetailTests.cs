using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.ValueObjects;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Services;
using MesTech.Domain.ValueObjects;
using AccountingAccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Tests.Unit.Accounting;

/// <summary>
/// Accounting edge case tests — FIFO, rounding/precision, trial balance,
/// reconciliation, and KDV (VAT) scenarios for Turkish e-commerce context.
/// Uses REAL domain entities from MesTech.Domain (no mocks).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AccountingEdgeCases")]
[Trait("Phase", "Dalga14")]
public class AccountingEdgeCaseDetailTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════════════
    // SECTION 1: FIFO Edge Cases (Tests 1-10)
    // ═══════════════════════════════════════════════════════════════════

    #region FIFO Edge Cases

    /// <summary>
    /// Test 1: FIFO with empty stock — SelectLotsForConsumption returns empty list.
    /// </summary>
    [Fact]
    public void Fifo_EmptyStock_ReturnsEmptySelection()
    {
        // Arrange
        var service = new StockCalculationService();
        var emptyLots = Enumerable.Empty<InventoryLot>();

        // Act
        var selected = service.SelectLotsForConsumption(emptyLots, 10m);

        // Assert
        selected.Should().BeEmpty();
        selected.Count.Should().Be(0);
    }

    /// <summary>
    /// Test 2: FIFO with single lot — cost equals lot cost exactly.
    /// </summary>
    [Fact]
    public void Fifo_SingleLot_CostEqualsLotCost()
    {
        // Arrange
        var service = new StockCalculationService();
        var lot = CreateLot("LOT-001", receivedQty: 100, remainingQty: 100,
            expiryDate: DateTime.UtcNow.AddDays(30));

        // Act
        var selected = service.SelectLotsForConsumption(new[] { lot }, 50m);

        // Assert
        selected.Should().HaveCount(1);
        selected.First().LotNumber.Should().Be("LOT-001");
        selected.First().RemainingQty.Should().Be(100m);
    }

    /// <summary>
    /// Test 3: FIFO with multiple lots, partial consumption spans across lots.
    /// </summary>
    [Fact]
    public void Fifo_MultipleLots_PartialConsumption_SelectsInOrder()
    {
        // Arrange
        var service = new StockCalculationService();
        var lot1 = CreateLot("LOT-001", remainingQty: 30,
            expiryDate: DateTime.UtcNow.AddDays(10));
        var lot2 = CreateLot("LOT-002", remainingQty: 50,
            expiryDate: DateTime.UtcNow.AddDays(20));
        var lot3 = CreateLot("LOT-003", remainingQty: 40,
            expiryDate: DateTime.UtcNow.AddDays(30));

        // Act — need 60, lot1 (30) + lot2 (50) should suffice
        var selected = service.SelectLotsForConsumption(new[] { lot1, lot2, lot3 }, 60m);

        // Assert
        selected.Should().HaveCount(2);
        selected.First().LotNumber.Should().Be("LOT-001"); // earliest expiry first
        selected.Last().LotNumber.Should().Be("LOT-002");
    }

    /// <summary>
    /// Test 4: FIFO sell more than available — still returns available lots (caller handles deficit).
    /// </summary>
    [Fact]
    public void Fifo_SellMoreThanAvailable_ReturnsAllAvailableLots()
    {
        // Arrange
        var service = new StockCalculationService();
        var lot1 = CreateLot("LOT-001", remainingQty: 20, expiryDate: DateTime.UtcNow.AddDays(5));
        var lot2 = CreateLot("LOT-002", remainingQty: 15, expiryDate: DateTime.UtcNow.AddDays(10));

        // Act — request 100, only 35 available
        var selected = service.SelectLotsForConsumption(new[] { lot1, lot2 }, 100m);

        // Assert
        selected.Should().HaveCount(2);
        selected.Sum(l => l.RemainingQty).Should().Be(35m);
    }

    /// <summary>
    /// Test 5: FIFO with zero-cost lot — lot is still selectable.
    /// </summary>
    [Fact]
    public void Fifo_ZeroCostLot_StillSelectable()
    {
        // Arrange
        var service = new StockCalculationService();
        var lot = CreateLot("LOT-FREE", remainingQty: 10,
            expiryDate: DateTime.UtcNow.AddDays(30));
        // Zero-cost represented by WAC = 0 in calculation
        var wac = service.CalculateWAC(0, 0m, 10, 0m);

        // Act
        var selected = service.SelectLotsForConsumption(new[] { lot }, 5m);

        // Assert
        selected.Should().HaveCount(1);
        wac.Should().Be(0m);
    }

    /// <summary>
    /// Test 6: FIFO lot expiry (FEFO variant) — expired lots are skipped.
    /// </summary>
    [Fact]
    public void Fifo_ExpiredLots_AreSkipped_ByStatus()
    {
        // Arrange
        var service = new StockCalculationService();
        var expiredLot = CreateLot("LOT-EXPIRED", remainingQty: 100,
            expiryDate: DateTime.UtcNow.AddDays(-5), status: LotStatus.Expired);
        var validLot = CreateLot("LOT-VALID", remainingQty: 50,
            expiryDate: DateTime.UtcNow.AddDays(30));

        // Act
        var selected = service.SelectLotsForConsumption(new[] { expiredLot, validLot }, 30m);

        // Assert — expired lot (status=Expired) is not Open, so it is skipped
        selected.Should().HaveCount(1);
        selected.First().LotNumber.Should().Be("LOT-VALID");
    }

    /// <summary>
    /// Test 7: FIFO with return (re-add to stock) — lot quantity increases, then re-consumed.
    /// </summary>
    [Fact]
    public void Fifo_Return_ReAddToStock_ThenReConsume()
    {
        // Arrange
        var lot = CreateLot("LOT-RETURN", receivedQty: 50, remainingQty: 10,
            expiryDate: DateTime.UtcNow.AddDays(15));

        // Act — simulate return by increasing remaining qty
        lot.RemainingQty += 5; // 5 units returned
        lot.Status = LotStatus.Open;

        // Then consume again
        lot.Consume(12);

        // Assert
        lot.RemainingQty.Should().Be(3m); // 10 + 5 - 12
        lot.Status.Should().Be(LotStatus.Open);
    }

    /// <summary>
    /// Test 8: FIFO cross-tenant isolation — lots from different tenants are separate.
    /// </summary>
    [Fact]
    public void Fifo_CrossTenantIsolation_LotsAreSeparate()
    {
        // Arrange
        var service = new StockCalculationService();
        var lotTenantA = CreateLot("LOT-A", remainingQty: 100, tenantId: TenantA);
        var lotTenantB = CreateLot("LOT-B", remainingQty: 200, tenantId: TenantB);

        // Act — filter by tenant A only
        var tenantALots = new[] { lotTenantA, lotTenantB }
            .Where(l => l.TenantId == TenantA);
        var selected = service.SelectLotsForConsumption(tenantALots, 50m);

        // Assert
        selected.Should().HaveCount(1);
        selected.First().TenantId.Should().Be(TenantA);
        selected.First().LotNumber.Should().Be("LOT-A");
    }

    /// <summary>
    /// Test 9: FIFO with concurrent consumption — Consume throws when exceeding remaining.
    /// </summary>
    [Fact]
    public void Fifo_ConcurrentConsumption_ThrowsOnExceedingRemaining()
    {
        // Arrange
        var lot = CreateLot("LOT-CONC", remainingQty: 10);

        // Act — first consume succeeds
        lot.Consume(7);
        lot.RemainingQty.Should().Be(3m);

        // Second consume exceeds remaining
        var act = () => lot.Consume(5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Cannot consume");
    }

    /// <summary>
    /// Test 10: FIFO rounding — 3 items at 10.01 TL, WAC should handle decimals.
    /// </summary>
    [Fact]
    public void Fifo_Rounding_ThreeItemsAtFractionalCost()
    {
        // Arrange
        var service = new StockCalculationService();

        // Act — 3 items at 10.01 each
        var wac = service.CalculateWAC(0, 0m, 3, 10.01m);

        // Assert — should be exactly 10.01 (no rounding issues)
        wac.Should().Be(10.01m);

        // Adding more at a different price
        var wac2 = service.CalculateWAC(3, 10.01m, 2, 10.02m);
        var expected = (3 * 10.01m + 2 * 10.02m) / 5;
        wac2.Should().Be(expected);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // SECTION 2: Rounding & Precision (Tests 11-20)
    // ═══════════════════════════════════════════════════════════════════

    #region Rounding & Precision

    /// <summary>
    /// Test 11: 0.01 TL rounding in commission calculation.
    /// </summary>
    [Fact]
    public void Rounding_CommissionOnePenny_RoundsCorrectly()
    {
        // Arrange
        var commission = CreateCommission(CommissionType.Percentage, rate: 1m); // 1%

        // Act — 1% of 1.00 TL = 0.01
        var result = commission.Calculate(1.00m);

        // Assert
        result.Should().Be(0.01m);
        (result * 100).Should().Be(1m); // 1 kurus
    }

    /// <summary>
    /// Test 12: 1/3 split — 33.33 + 33.33 + 33.34 = 100.00 TL.
    /// </summary>
    [Fact]
    public void Rounding_OneThirdSplit_SumEquals100()
    {
        // Arrange — split 100 TL into 3 parts
        var total = 100.00m;
        var part = Math.Round(total / 3, 2); // 33.33
        var remainder = total - (part * 2);  // 33.34

        // Act
        var money1 = Money.TRY(part);
        var money2 = Money.TRY(part);
        var money3 = Money.TRY(remainder);
        var sum = money1.Add(money2).Add(money3);

        // Assert
        sum.Amount.Should().Be(100.00m);
        part.Should().Be(33.33m);
        remainder.Should().Be(33.34m);
    }

    /// <summary>
    /// Test 13: KDV calculation rounding — 18% of 99.99 TL.
    /// </summary>
    [Fact]
    public void Rounding_KdvOn99_99_RoundsToKurus()
    {
        // Arrange
        var pricing = new PricingService();
        var basePrice = 99.99m;
        var taxRate = 0.18m;

        // Act
        var priceWithKdv = pricing.CalculatePriceWithTax(basePrice, taxRate);
        var kdvAmount = Math.Round(basePrice * taxRate, 2);

        // Assert — 99.99 * 1.18 = 117.9882
        priceWithKdv.Should().Be(117.9882m);
        kdvAmount.Should().Be(18.00m); // 99.99 * 0.18 = 17.9982 ~ 18.00
    }

    /// <summary>
    /// Test 14: Multi-currency conversion rounding.
    /// </summary>
    [Fact]
    public void Rounding_MultiCurrencyConversion_DoesNotLosePrecision()
    {
        // Arrange — 100 USD * 32.4567 exchange rate
        var usd = Money.USD(100m);
        var exchangeRate = 32.4567m;
        var tryAmount = Math.Round(usd.Amount * exchangeRate, 2);

        // Act
        var tlMoney = Money.TRY(tryAmount);

        // Assert
        tlMoney.Amount.Should().Be(3245.67m);
        tlMoney.Currency.Should().Be("TRY");
    }

    /// <summary>
    /// Test 15: Commission percentage edge cases — 0%, 100%, 99.99%.
    /// </summary>
    [Theory]
    [InlineData(0, 1000, 0)]         // 0% commission
    [InlineData(100, 1000, 1000)]    // 100% commission
    [InlineData(99.99, 1000, 999.9)] // 99.99% commission
    public void Rounding_CommissionPercentageEdges(decimal rate, decimal saleAmount, decimal expectedCommission)
    {
        // Arrange
        var commission = CreateCommission(CommissionType.Percentage, rate: rate);

        // Act
        var result = commission.Calculate(saleAmount);

        // Assert
        result.Should().Be(expectedCommission);
        result.Should().BeGreaterThanOrEqualTo(0m);
    }

    /// <summary>
    /// Test 16: Negative rounding — Money allows negative amounts.
    /// </summary>
    [Fact]
    public void Rounding_NegativeAmount_MoneySubtractHandled()
    {
        // Arrange
        var small = Money.TRY(0.005m);
        var zero = Money.TRY(0m);

        // Act
        var negative = zero.Subtract(small);
        var roundedNegative = Math.Round(negative.Amount, 2);

        // Assert
        negative.Amount.Should().Be(-0.005m);
        roundedNegative.Should().Be(0.00m); // MidpointRounding.ToEven (default) rounds -0.005 to 0.00 (banker's rounding)
    }

    /// <summary>
    /// Test 17: Large amount precision — 1,000,000.00 TL x 18% KDV.
    /// </summary>
    [Fact]
    public void Rounding_LargeAmount_KdvPrecisionMaintained()
    {
        // Arrange
        var pricing = new PricingService();
        var largeAmount = 1_000_000.00m;

        // Act
        var withTax = pricing.CalculatePriceWithTax(largeAmount, 0.18m);
        var kdv = largeAmount * 0.18m;

        // Assert
        withTax.Should().Be(1_180_000.00m);
        kdv.Should().Be(180_000.00m);
    }

    /// <summary>
    /// Test 18: Penny test — 0.01 TL x 1000 items = 10.00 TL exactly.
    /// </summary>
    [Fact]
    public void Rounding_PennyTest_0_01_Times1000()
    {
        // Arrange
        var itemPrice = 0.01m;
        var quantity = 1000;

        // Act
        var total = itemPrice * quantity;
        var money = Money.TRY(total);

        // Assert
        money.Amount.Should().Be(10.00m);
        total.Should().Be(10.00m);
    }

    /// <summary>
    /// Test 19: Cross-platform commission difference — Trendyol 12% vs N11 10%.
    /// </summary>
    [Fact]
    public void Rounding_CrossPlatformCommissionDifference()
    {
        // Arrange
        var trendyolComm = CreateCommission(CommissionType.Percentage, rate: 12m,
            platform: PlatformType.Trendyol);
        var n11Comm = CreateCommission(CommissionType.Percentage, rate: 10m,
            platform: PlatformType.N11);
        var saleAmount = 500m;

        // Act
        var trendyolFee = trendyolComm.Calculate(saleAmount);
        var n11Fee = n11Comm.Calculate(saleAmount);
        var difference = trendyolFee - n11Fee;

        // Assert
        trendyolFee.Should().Be(60m);
        n11Fee.Should().Be(50m);
        difference.Should().Be(10m);
    }

    /// <summary>
    /// Test 20: Withholding tax rounding — 2/10 KDV tevkifat on 100 TL KDV.
    /// </summary>
    [Fact]
    public void Rounding_WithholdingTax_TevkifatCalculation()
    {
        // Arrange — tevkifat oranı 2/10 (yaygın oran)
        var kdvAmount = 100m;
        var tevkifatRate = 2m / 10m; // 0.20

        // Act
        var withheld = Math.Round(kdvAmount * tevkifatRate, 2);
        var netKdv = kdvAmount - withheld;

        // Assert
        withheld.Should().Be(20m);
        netKdv.Should().Be(80m);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // SECTION 3: Trial Balance (Tests 21-30)
    // ═══════════════════════════════════════════════════════════════════

    #region Trial Balance

    /// <summary>
    /// Test 21: Empty trial balance — total debit=0, credit=0.
    /// </summary>
    [Fact]
    public void TrialBalance_Empty_DebitAndCreditAreZero()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-001", "Test Musteri");

        // Act
        var totalDebit = account.Transactions.Sum(t => t.DebitAmount);
        var totalCredit = account.Transactions.Sum(t => t.CreditAmount);

        // Assert
        totalDebit.Should().Be(0m);
        totalCredit.Should().Be(0m);
        account.Balance.Should().Be(0m);
    }

    /// <summary>
    /// Test 22: Single entry — debit == credit for balanced journal.
    /// </summary>
    [Fact]
    public void TrialBalance_SingleEntry_DebitEqualsCreditForBalancedJournal()
    {
        // Arrange — simulate balanced double entry via two accounts
        var customerAcc = CreateCustomerAccount("MUS-002", "Balanced Co");
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act — sale creates debit on customer account
        customerAcc.RecordSale(invoiceId, orderId, 1000m, "FTR-001");
        // Collection creates credit
        customerAcc.RecordCollection(1000m, "TAH-001");

        // Assert — balanced
        var totalDebit = customerAcc.Transactions.Sum(t => t.DebitAmount);
        var totalCredit = customerAcc.Transactions.Sum(t => t.CreditAmount);
        customerAcc.Balance.Should().Be(0m);
        totalDebit.Should().Be(totalCredit);
    }

    /// <summary>
    /// Test 23: 100 entries — still balanced after many transactions.
    /// </summary>
    [Fact]
    public void TrialBalance_100Entries_StillBalanced()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-003", "Heavy User");
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act — 100 sales + 100 matching collections
        for (int i = 0; i < 100; i++)
        {
            account.RecordSale(invoiceId, orderId, 10m, $"FTR-{i:D3}");
            account.RecordCollection(10m, $"TAH-{i:D3}");
        }

        // Assert
        account.Balance.Should().Be(0m);
        account.Transactions.Should().HaveCount(200);
    }

    /// <summary>
    /// Test 24: Entry with zero amount — valid but no balance change.
    /// </summary>
    [Fact]
    public void TrialBalance_ZeroAmountEntry_NoBalanceChange()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-004", "Zero Entry");

        // Act
        account.AddTransaction(new AccountTransaction
        {
            TenantId = TenantA,
            Type = TransactionType.Adjustment,
            DebitAmount = 0m,
            CreditAmount = 0m,
            Description = "Zero adjustment"
        });

        // Assert
        account.Balance.Should().Be(0m);
        account.Transactions.Should().HaveCount(1);
    }

    /// <summary>
    /// Test 25: Voided/reversed entry — original + reversal net to zero.
    /// </summary>
    [Fact]
    public void TrialBalance_VoidedEntry_NetsToZero()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-005", "Void Test");
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act — original sale
        account.RecordSale(invoiceId, orderId, 500m, "FTR-VOID");
        account.Balance.Should().Be(500m);

        // Reverse with credit entry
        account.AddTransaction(new AccountTransaction
        {
            TenantId = TenantA,
            Type = TransactionType.Adjustment,
            DebitAmount = 0m,
            CreditAmount = 500m,
            Description = "Reversal of FTR-VOID"
        });

        // Assert
        account.Balance.Should().Be(0m);
        account.Transactions.Should().HaveCount(2);
    }

    /// <summary>
    /// Test 26: Multi-tenant trial balance isolation.
    /// </summary>
    [Fact]
    public void TrialBalance_MultiTenantIsolation_BalancesIndependent()
    {
        // Arrange
        var accountA = CreateCustomerAccount("MUS-A", "Tenant A", tenantId: TenantA);
        var accountB = CreateCustomerAccount("MUS-B", "Tenant B", tenantId: TenantB);

        // Act
        accountA.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "FTR-A");
        accountB.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 2000m, "FTR-B");

        // Assert — each tenant sees only their balance
        accountA.Balance.Should().Be(1000m);
        accountB.Balance.Should().Be(2000m);
        accountA.TenantId.Should().NotBe(accountB.TenantId);
    }

    /// <summary>
    /// Test 27: Date-filtered trial balance — only March entries counted.
    /// </summary>
    [Fact]
    public void TrialBalance_DateFiltered_OnlyMarchEntries()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-007", "Date Filter");
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act — add transactions with different dates
        var tx1 = account.RecordSale(invoiceId, orderId, 100m, "FTR-FEB");
        tx1.TransactionDate = new DateTime(2026, 2, 15); // February

        var tx2 = account.RecordSale(invoiceId, orderId, 200m, "FTR-MAR1");
        tx2.TransactionDate = new DateTime(2026, 3, 10); // March

        var tx3 = account.RecordSale(invoiceId, orderId, 300m, "FTR-MAR2");
        tx3.TransactionDate = new DateTime(2026, 3, 25); // March

        // Assert — filter for March
        var marchStart = new DateTime(2026, 3, 1);
        var marchEnd = new DateTime(2026, 3, 31, 23, 59, 59);
        var marchTransactions = account.Transactions
            .Where(t => t.TransactionDate >= marchStart && t.TransactionDate <= marchEnd);

        marchTransactions.Sum(t => t.DebitAmount).Should().Be(500m); // 200 + 300
        marchTransactions.Count().Should().Be(2);
    }

    /// <summary>
    /// Test 28: Trial balance with opening balances.
    /// </summary>
    [Fact]
    public void TrialBalance_WithOpeningBalance_IncludedInTotal()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-008", "Opening Balance");

        // Act — opening balance entry
        account.AddTransaction(new AccountTransaction
        {
            TenantId = TenantA,
            Type = TransactionType.OpeningBalance,
            DebitAmount = 5000m,
            CreditAmount = 0m,
            Description = "Acilis bakiyesi 2026"
        });

        // Then normal sale
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "FTR-OPEN");

        // Assert
        account.Balance.Should().Be(6000m);
        account.Transactions.Should().HaveCount(2);
        account.Transactions.First().Type.Should().Be(TransactionType.OpeningBalance);
    }

    /// <summary>
    /// Test 29: Trial balance after journal entry deletion (soft delete simulation).
    /// </summary>
    [Fact]
    public void TrialBalance_AfterEntryDeletion_BalanceStillReflectsActiveEntries()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-009", "Deletion Test");
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "FTR-DEL1");
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 500m, "FTR-DEL2");

        // Act — mark one as deleted (soft delete)
        var txToDelete = account.Transactions.First();
        txToDelete.IsDeleted = true;
        txToDelete.DeletedAt = DateTime.UtcNow;

        // Active-only balance
        var activeBalance = account.Transactions
            .Where(t => !t.IsDeleted)
            .Sum(t => t.DebitAmount - t.CreditAmount);

        // Assert
        activeBalance.Should().Be(500m);
        account.Transactions.Count(t => t.IsDeleted).Should().Be(1);
    }

    /// <summary>
    /// Test 30: Negative journal entry amounts — credit exceeds debit.
    /// </summary>
    [Fact]
    public void TrialBalance_NegativeBalance_CreditExceedsDebit()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-010", "Negative Balance");

        // Act — only credits, no debits
        account.RecordCollection(1000m, "TAH-NEG1");
        account.RecordCollection(500m, "TAH-NEG2");

        // Assert — balance is negative (overpaid customer)
        account.Balance.Should().Be(-1500m);
        account.Transactions.All(t => t.CreditAmount > 0).Should().BeTrue();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // SECTION 4: Reconciliation (Tests 31-40)
    // ═══════════════════════════════════════════════════════════════════

    #region Reconciliation

    /// <summary>
    /// Test 31: Perfect match — settlement net amount == order total.
    /// </summary>
    [Fact]
    public void Reconciliation_PerfectMatch_SettlementEqualsOrder()
    {
        // Arrange
        var payment = CreatePlatformPayment(grossSales: 1000m, commission: 120m,
            shipping: 0m, returns: 0m, otherDeductions: 0m);

        // Act
        payment.CalculateNetAmount();

        // Assert
        payment.NetAmount.Should().Be(880m); // 1000 - 120
        payment.GrossSales.Should().Be(1000m);
        (payment.GrossSales - payment.TotalCommission).Should().Be(payment.NetAmount);
    }

    /// <summary>
    /// Test 32: Partial match — settlement less than order total.
    /// </summary>
    [Fact]
    public void Reconciliation_PartialMatch_SettlementLessThanOrder()
    {
        // Arrange
        var payment = CreatePlatformPayment(grossSales: 1000m, commission: 120m,
            shipping: 30m, returns: 0m, otherDeductions: 50m);

        // Act
        payment.CalculateNetAmount();

        // Assert
        payment.NetAmount.Should().Be(800m); // 1000 - 120 - 30 - 0 - 50
        payment.NetAmount.Should().BeLessThan(payment.GrossSales);
    }

    /// <summary>
    /// Test 33: Overpayment scenario — settlement greater than order total (refund credits).
    /// </summary>
    [Fact]
    public void Reconciliation_Overpayment_NetCanExceedExpected()
    {
        // Arrange — gross sales is high but deductions are zero (overpayment case)
        var payment = CreatePlatformPayment(grossSales: 2000m, commission: 0m,
            shipping: 0m, returns: 0m, otherDeductions: 0m);

        // Act
        payment.CalculateNetAmount();

        // Assert
        payment.NetAmount.Should().Be(2000m);
        payment.NetAmount.Should().Be(payment.GrossSales);
    }

    /// <summary>
    /// Test 34: Multiple orders in single settlement — aggregate correctly.
    /// </summary>
    [Fact]
    public void Reconciliation_MultipleOrdersInSingleSettlement()
    {
        // Arrange — settlement covers multiple orders
        var payment = CreatePlatformPayment(grossSales: 5000m, commission: 600m,
            shipping: 150m, returns: 200m, otherDeductions: 50m);
        payment.OrderCount = 10;

        // Act
        payment.CalculateNetAmount();

        // Assert
        payment.NetAmount.Should().Be(4000m); // 5000 - 600 - 150 - 200 - 50
        payment.OrderCount.Should().Be(10);
    }

    /// <summary>
    /// Test 35: Settlement with commission deduction — verify commission is subtracted.
    /// </summary>
    [Fact]
    public void Reconciliation_SettlementWithCommissionDeduction()
    {
        // Arrange
        var commission = CreateCommission(CommissionType.Percentage, rate: 15m,
            platform: PlatformType.Trendyol);
        var saleAmount = 1000m;
        var commissionAmount = commission.Calculate(saleAmount);

        var payment = CreatePlatformPayment(grossSales: saleAmount,
            commission: commissionAmount, shipping: 0m, returns: 0m, otherDeductions: 0m);

        // Act
        payment.CalculateNetAmount();

        // Assert
        commissionAmount.Should().Be(150m);
        payment.NetAmount.Should().Be(850m);
        payment.TotalCommission.Should().Be(150m);
    }

    /// <summary>
    /// Test 36: Unmatched settlement — no corresponding order (orphan payment).
    /// </summary>
    [Fact]
    public void Reconciliation_UnmatchedSettlement_NoOrderAssociation()
    {
        // Arrange
        var payment = CreatePlatformPayment(grossSales: 500m, commission: 50m);
        payment.OrderCount = 0; // no matching orders
        payment.CalculateNetAmount();

        // Act
        var isUnmatched = payment.OrderCount == 0 && payment.NetAmount > 0;

        // Assert
        isUnmatched.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    /// <summary>
    /// Test 37: Duplicate settlement detection — same period payments identified.
    /// </summary>
    [Fact]
    public void Reconciliation_DuplicateSettlement_DetectedByPeriod()
    {
        // Arrange
        var periodStart = new DateTime(2026, 3, 1);
        var periodEnd = new DateTime(2026, 3, 7);

        var payment1 = CreatePlatformPayment(grossSales: 1000m, commission: 100m);
        payment1.PeriodStart = periodStart;
        payment1.PeriodEnd = periodEnd;
        payment1.PlatformPaymentId = "PAY-001";

        var payment2 = CreatePlatformPayment(grossSales: 1000m, commission: 100m);
        payment2.PeriodStart = periodStart;
        payment2.PeriodEnd = periodEnd;
        payment2.PlatformPaymentId = "PAY-001"; // same ID = duplicate

        // Act
        var isDuplicate = payment1.PlatformPaymentId == payment2.PlatformPaymentId
            && payment1.PeriodStart == payment2.PeriodStart;

        // Assert
        isDuplicate.Should().BeTrue();
        payment1.PlatformPaymentId.Should().Be(payment2.PlatformPaymentId);
    }

    /// <summary>
    /// Test 38: Settlement date vs order date mismatch — payment scheduled after delivery.
    /// </summary>
    [Fact]
    public void Reconciliation_SettlementDateMismatch_PaymentAfterOrder()
    {
        // Arrange
        var orderDate = new DateTime(2026, 3, 1);
        var payment = CreatePlatformPayment(grossSales: 500m, commission: 50m);
        payment.PeriodStart = orderDate;
        payment.PeriodEnd = new DateTime(2026, 3, 7);
        payment.ScheduledPaymentDate = new DateTime(2026, 3, 14); // 1 week after period end

        // Act
        var daysBetween = (payment.ScheduledPaymentDate!.Value - payment.PeriodEnd).Days;

        // Assert
        daysBetween.Should().Be(7);
        payment.ScheduledPaymentDate.Should().BeAfter(payment.PeriodEnd);
    }

    /// <summary>
    /// Test 39: Multi-currency settlement — different currencies cannot be added via Money.
    /// </summary>
    [Fact]
    public void Reconciliation_MultiCurrency_ThrowsOnMismatch()
    {
        // Arrange
        var tryPayment = Money.TRY(1000m);
        var usdPayment = Money.USD(50m);

        // Act
        var act = () => tryPayment.Add(usdPayment);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Cannot add TRY and USD");
    }

    /// <summary>
    /// Test 40: Settlement reversal/refund — MarkAsFailed updates status.
    /// </summary>
    [Fact]
    public void Reconciliation_SettlementReversal_MarkedAsFailed()
    {
        // Arrange
        var payment = CreatePlatformPayment(grossSales: 1000m, commission: 100m);
        payment.CalculateNetAmount();
        payment.MarkAsCompleted("BANK-REF-001");

        // Act — reversal scenario
        var refundPayment = CreatePlatformPayment(grossSales: 0m, commission: 0m);
        refundPayment.TotalReturnDeduction = 1000m;
        refundPayment.CalculateNetAmount();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Completed);
        refundPayment.NetAmount.Should().Be(-1000m); // negative = refund
        refundPayment.NetAmount.Should().BeLessThan(0m);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // SECTION 5: KDV (VAT) Scenarios (Tests 41-50)
    // ═══════════════════════════════════════════════════════════════════

    #region KDV (VAT) Scenarios

    /// <summary>
    /// Test 41: Standard 18% KDV on 1000 TL sale.
    /// </summary>
    [Fact]
    public void Kdv_Standard18Percent_On1000TL()
    {
        // Arrange
        var pricing = new PricingService();
        var basePrice = 1000m;

        // Act
        var withKdv = pricing.CalculatePriceWithTax(basePrice, 0.18m);
        var kdvAmount = basePrice * 0.18m;

        // Assert
        withKdv.Should().Be(1180m);
        kdvAmount.Should().Be(180m);
    }

    /// <summary>
    /// Test 42: Reduced 8% KDV (food category).
    /// </summary>
    [Fact]
    public void Kdv_Reduced8Percent_FoodCategory()
    {
        // Arrange
        var pricing = new PricingService();
        var line = new InvoiceLine
        {
            TenantId = TenantA,
            ProductName = "Zeytinyagi",
            Quantity = 2,
            UnitPrice = 250m,
            TaxRate = 0.08m
        };

        // Act
        line.CalculateLineTotal();

        // Assert
        line.TaxAmount.Should().Be(40m);  // (250*2) * 0.08
        line.LineTotal.Should().Be(540m);  // 500 + 40
    }

    /// <summary>
    /// Test 43: Exempt 0% KDV (export — e-Ihracat).
    /// </summary>
    [Fact]
    public void Kdv_ExemptZeroPercent_Export()
    {
        // Arrange
        var pricing = new PricingService();
        var exportPrice = 5000m;

        // Act
        var withKdv = pricing.CalculatePriceWithTax(exportPrice, 0m);
        var kdvAmount = exportPrice * 0m;

        // Assert
        withKdv.Should().Be(5000m); // no change
        kdvAmount.Should().Be(0m);
    }

    /// <summary>
    /// Test 44: KDV on commission — Trendyol charges KDV on its commission.
    /// </summary>
    [Fact]
    public void Kdv_OnCommission_TrendyolChargesKdvOnCommission()
    {
        // Arrange
        var commission = CreateCommission(CommissionType.Percentage, rate: 12m,
            platform: PlatformType.Trendyol);
        var saleAmount = 1000m;
        var commissionAmount = commission.Calculate(saleAmount); // 120 TL
        var kdvOnCommission = Math.Round(commissionAmount * 0.18m, 2); // KDV on the commission itself

        // Act
        var totalCommissionWithKdv = commissionAmount + kdvOnCommission;

        // Assert
        commissionAmount.Should().Be(120m);
        kdvOnCommission.Should().Be(21.60m);
        totalCommissionWithKdv.Should().Be(141.60m);
    }

    /// <summary>
    /// Test 45: Input KDV from supplier invoices — credit for purchases.
    /// </summary>
    [Fact]
    public void Kdv_InputFromSupplierInvoice_CreditAvailable()
    {
        // Arrange
        var supplierAccount = CreateSupplierAccount("TED-001", "Tedarikci A");
        var purchaseAmount = 800m;
        var kdvRate = 0.18m;
        var inputKdv = Math.Round(purchaseAmount * kdvRate, 2);

        // Act
        supplierAccount.RecordPurchase(Guid.NewGuid(), purchaseAmount + inputKdv, "FATURA-TED-001");

        // Assert
        inputKdv.Should().Be(144m);
        supplierAccount.Balance.Should().Be(-(purchaseAmount + inputKdv)); // negative = we owe supplier
        supplierAccount.Transactions.Should().HaveCount(1);
    }

    /// <summary>
    /// Test 46: Net KDV payable = Output KDV - Input KDV.
    /// </summary>
    [Fact]
    public void Kdv_NetPayable_OutputMinusInput()
    {
        // Arrange
        var salesAmount = 10_000m;
        var purchaseAmount = 6_000m;
        var kdvRate = 0.18m;

        // Act
        var outputKdv = Math.Round(salesAmount * kdvRate, 2);   // KDV on sales
        var inputKdv = Math.Round(purchaseAmount * kdvRate, 2);  // KDV on purchases
        var netKdvPayable = outputKdv - inputKdv;

        // Assert
        outputKdv.Should().Be(1800m);
        inputKdv.Should().Be(1080m);
        netKdvPayable.Should().Be(720m); // owed to tax authority
    }

    /// <summary>
    /// Test 47: KDV on return/refund — customer account credited, KDV reversed.
    /// </summary>
    [Fact]
    public void Kdv_OnReturn_KdvReversed()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-KDV", "KDV Return Test");
        var originalSale = 1180m; // 1000 + 180 KDV
        var kdvRate = 0.18m;

        // Sale
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), originalSale, "FTR-KDV1");

        // Act — return
        var returnAmount = originalSale; // full return
        account.RecordReturn(Guid.NewGuid(), returnAmount, PlatformType.Trendyol);

        // Assert
        account.Balance.Should().Be(0m);
        var returnKdv = Math.Round(returnAmount / (1 + kdvRate) * kdvRate, 2);
        returnKdv.Should().Be(180m);
    }

    /// <summary>
    /// Test 48: KDV period boundary — last day of month.
    /// </summary>
    [Fact]
    public void Kdv_PeriodBoundary_LastDayOfMonth()
    {
        // Arrange
        var account = CreateCustomerAccount("MUS-PERIOD", "Period Boundary");
        var marchEnd = new DateTime(2026, 3, 31, 23, 59, 59);
        var aprilStart = new DateTime(2026, 4, 1, 0, 0, 0);

        // Act
        var txMarch = account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "FTR-MAR");
        txMarch.TransactionDate = marchEnd;

        var txApril = account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 2000m, "FTR-APR");
        txApril.TransactionDate = aprilStart;

        // Assert — March KDV period
        var marchTxs = account.Transactions
            .Where(t => t.TransactionDate.Month == 3 && t.TransactionDate.Year == 2026);
        var aprilTxs = account.Transactions
            .Where(t => t.TransactionDate.Month == 4 && t.TransactionDate.Year == 2026);

        marchTxs.Sum(t => t.DebitAmount).Should().Be(1000m);
        aprilTxs.Sum(t => t.DebitAmount).Should().Be(2000m);
    }

    /// <summary>
    /// Test 49: KDV with withholding (tevkifat) — buyer withholds portion of KDV.
    /// </summary>
    [Fact]
    public void Kdv_WithTevkifat_BuyerWithholdsPortion()
    {
        // Arrange — 5/10 tevkifat on temizlik hizmeti
        var baseAmount = 10_000m;
        var kdvRate = 0.18m;
        var tevkifatRate = 5m / 10m; // 0.50

        // Act
        var fullKdv = Math.Round(baseAmount * kdvRate, 2);
        var withheldKdv = Math.Round(fullKdv * tevkifatRate, 2);
        var sellerCollectsKdv = fullKdv - withheldKdv;
        var totalInvoice = baseAmount + sellerCollectsKdv;

        // Assert
        fullKdv.Should().Be(1800m);
        withheldKdv.Should().Be(900m);
        sellerCollectsKdv.Should().Be(900m);
        totalInvoice.Should().Be(10_900m); // seller receives base + half-KDV
    }

    /// <summary>
    /// Test 50: KDV rounding to kurus — fractional kurus values.
    /// </summary>
    [Fact]
    public void Kdv_RoundingToKurus_FractionalValues()
    {
        // Arrange — 18% of 33.33 TL
        var basePrice = 33.33m;
        var kdvRate = 0.18m;

        // Act
        var kdvExact = basePrice * kdvRate; // 5.9994
        var kdvRounded = Math.Round(kdvExact, 2);
        var totalRounded = Math.Round(basePrice + kdvRounded, 2);

        // Assert
        kdvExact.Should().Be(5.9994m);
        kdvRounded.Should().Be(6.00m);
        totalRounded.Should().Be(39.33m);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // BONUS: Additional Edge Cases (Tests 51-55)
    // ═══════════════════════════════════════════════════════════════════

    #region Bonus Edge Cases

    /// <summary>
    /// Test 51: WAC with zero total stock — returns zero (no division by zero).
    /// </summary>
    [Fact]
    public void Wac_ZeroTotalStock_ReturnsZero()
    {
        // Arrange
        var service = new StockCalculationService();

        // Act — current=0 + added=0 = 0 total
        var wac = service.CalculateWAC(0, 100m, 0, 50m);

        // Assert
        wac.Should().Be(0m);
    }

    /// <summary>
    /// Test 52: Supplier overdue balance calculation.
    /// </summary>
    [Fact]
    public void Supplier_OverdueBalance_CalculatedCorrectly()
    {
        // Arrange
        var supplier = CreateSupplierAccount("TED-OVERDUE", "Overdue Supplier");
        var tx = supplier.RecordPayment(500m, "ODE-001",
            dueDate: DateTime.UtcNow.AddDays(-10)); // overdue
        supplier.RecordPayment(300m, "ODE-002",
            dueDate: DateTime.UtcNow.AddDays(10)); // not overdue

        // Act
        var overdueBalance = supplier.OverdueBalance(DateTime.UtcNow);

        // Assert
        overdueBalance.Should().Be(500m); // only the overdue payment
        supplier.Transactions.Should().HaveCount(2);
    }

    /// <summary>
    /// Test 53: InvoiceLine discount + tax calculation edge case.
    /// </summary>
    [Fact]
    public void InvoiceLine_DiscountPlusTax_CalculatesCorrectly()
    {
        // Arrange
        var line = new InvoiceLine
        {
            TenantId = TenantA,
            ProductName = "Indirimli Urun",
            Quantity = 3,
            UnitPrice = 100m,
            TaxRate = 0.18m,
            DiscountAmount = 50m // 50 TL discount on the line
        };

        // Act
        line.CalculateLineTotal();

        // Assert — subtotal = 3*100 - 50 = 250, tax = 250*0.18 = 45
        line.TaxAmount.Should().Be(45m);
        line.LineTotal.Should().Be(295m); // 250 + 45
    }

    /// <summary>
    /// Test 54: PlatformPayment with negative gross sales throws.
    /// </summary>
    [Fact]
    public void PlatformPayment_NegativeGrossSales_Throws()
    {
        // Arrange
        var payment = new PlatformPayment
        {
            TenantId = TenantA,
            Platform = PlatformType.Trendyol,
            GrossSales = -100m,
        };

        // Act
        var act = () => payment.CalculateNetAmount();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("non-negative");
    }

    /// <summary>
    /// Test 55: GLTransaction reconciliation state transition.
    /// </summary>
    [Fact]
    public void GLTransaction_Reconcile_SetsIsReconciledTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var glTx = GLTransaction.Create(
            TenantA,
            GLTransactionType.Income,
            1500m,
            "Trendyol weekly settlement",
            userId,
            referenceNumber: "TRD-2026-W12");

        // Act
        glTx.IsReconciled.Should().BeFalse();
        glTx.Reconcile();

        // Assert
        glTx.IsReconciled.Should().BeTrue();
        glTx.Amount.Should().Be(1500m);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════

    #region Helpers

    private static InventoryLot CreateLot(
        string lotNumber,
        decimal receivedQty = 100m,
        decimal remainingQty = 100m,
        DateTime? expiryDate = null,
        LotStatus status = LotStatus.Open,
        Guid? tenantId = null)
    {
        return new InventoryLot
        {
            TenantId = tenantId ?? TenantA,
            ProductId = Guid.NewGuid(),
            LotNumber = lotNumber,
            ReceivedQty = receivedQty,
            RemainingQty = remainingQty,
            ExpiryDate = expiryDate,
            Status = status,
            CreatedDate = DateTime.UtcNow
        };
    }

    private static PlatformCommission CreateCommission(
        CommissionType type,
        decimal rate,
        PlatformType platform = PlatformType.Trendyol,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        return new PlatformCommission
        {
            TenantId = TenantA,
            Platform = platform,
            Type = type,
            Rate = rate,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30)
        };
    }

    private static CustomerAccount CreateCustomerAccount(
        string accountCode,
        string customerName,
        Guid? tenantId = null)
    {
        return new CustomerAccount
        {
            TenantId = tenantId ?? TenantA,
            CustomerId = Guid.NewGuid(),
            AccountCode = accountCode,
            CustomerName = customerName,
            Currency = "TRY",
            IsActive = true
        };
    }

    private static SupplierAccount CreateSupplierAccount(
        string accountCode,
        string supplierName,
        Guid? tenantId = null)
    {
        return new SupplierAccount
        {
            TenantId = tenantId ?? TenantA,
            SupplierId = Guid.NewGuid(),
            AccountCode = accountCode,
            SupplierName = supplierName,
            Currency = "TRY",
            IsActive = true
        };
    }

    private static PlatformPayment CreatePlatformPayment(
        decimal grossSales,
        decimal commission,
        decimal shipping = 0m,
        decimal returns = 0m,
        decimal otherDeductions = 0m)
    {
        return new PlatformPayment
        {
            TenantId = TenantA,
            Platform = PlatformType.Trendyol,
            GrossSales = grossSales,
            TotalCommission = commission,
            TotalShippingCost = shipping,
            TotalReturnDeduction = returns,
            OtherDeductions = otherDeductions,
            Currency = "TRY",
            PeriodStart = DateTime.UtcNow.AddDays(-7),
            PeriodEnd = DateTime.UtcNow
        };
    }

    #endregion
}
