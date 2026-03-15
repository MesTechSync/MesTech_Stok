using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

/// <summary>
/// ReconciliationService testleri — Phase C: TASK C7.
/// SettlementLine + BankTransaction eslestirme senaryolari.
/// Guven skoru: 0.95-1.00 (OrderId + exact amount), 0.80-0.94 (OrderId + ~1%),
///              0.60-0.79 (amount + date), &lt;0.60 (no match).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Reconciliation")]
[Trait("Phase", "PhaseC")]
public class ReconciliationServiceTests
{
    private readonly ReconciliationService _sut = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // -- Helpers ----------------------------------------------------------------

    private SettlementLine CreateSettlementLine(
        string? orderId = null,
        decimal netAmount = 1000m,
        DateTime? createdAt = null)
    {
        var batchId = Guid.NewGuid();
        var line = SettlementLine.Create(
            _tenantId,
            batchId,
            orderId,
            grossAmount: netAmount * 1.2m,
            commissionAmount: netAmount * 0.10m,
            serviceFee: netAmount * 0.05m,
            cargoDeduction: netAmount * 0.03m,
            refundDeduction: 0m,
            netAmount: netAmount);

        if (createdAt.HasValue)
        {
            // Use reflection to set CreatedAt since it's set in the factory method
            typeof(SettlementLine).BaseType!.BaseType!.GetProperty("CreatedAt")!
                .SetValue(line, createdAt.Value);
        }

        return line;
    }

    private BankTransaction CreateBankTransaction(
        decimal amount = 1000m,
        DateTime? transactionDate = null,
        string description = "Platform payment",
        string? referenceNumber = null)
    {
        return BankTransaction.Create(
            _tenantId,
            bankAccountId: Guid.NewGuid(),
            transactionDate: transactionDate ?? DateTime.UtcNow,
            amount: amount,
            description: description,
            referenceNumber: referenceNumber);
    }

    // ===========================================================================
    // TEST 1: ExactMatch_HighConfidence
    // OrderId exact match + exact amount → confidence >= 0.95
    // ===========================================================================

    [Fact]
    public void ExactMatch_HighConfidence()
    {
        // Arrange
        var orderId = "ORD-12345";
        var line = CreateSettlementLine(orderId: orderId, netAmount: 500m);
        var tx = CreateBankTransaction(
            amount: 500m,
            description: $"Platform payment {orderId}",
            referenceNumber: orderId);

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line },
            new[] { tx });

        // Assert
        result.Should().HaveCount(1);
        result[0].Confidence.Should().BeGreaterOrEqualTo(0.95m,
            "OrderId exact match + exact amount should yield high confidence");
        result[0].Status.Should().Be(ReconciliationStatus.AutoMatched);
        result[0].SettlementBatchId.Should().Be(line.SettlementBatchId);
        result[0].BankTransactionId.Should().Be(tx.Id);
    }

    // ===========================================================================
    // TEST 2: AmountWithinTolerance_MediumConfidence
    // OrderId match + amount within 1% → confidence 0.80-0.94
    // ===========================================================================

    [Fact]
    public void AmountWithinTolerance_MediumConfidence()
    {
        // Arrange — amount off by ~0.5%
        var orderId = "ORD-67890";
        var line = CreateSettlementLine(orderId: orderId, netAmount: 1000m);
        var tx = CreateBankTransaction(
            amount: 995m, // 0.5% difference
            description: $"TY {orderId}");

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line },
            new[] { tx });

        // Assert
        result.Should().HaveCount(1);
        result[0].Confidence.Should().BeInRange(0.80m, 0.94m,
            "OrderId match + amount within 1% should yield medium-high confidence");
        result[0].Status.Should().Be(ReconciliationStatus.AutoMatched);
    }

    // ===========================================================================
    // TEST 3: DateOnly_LowConfidence
    // No OrderId, amount match + date within 3 days → confidence 0.60-0.79
    // ===========================================================================

    [Fact]
    public void DateOnly_LowConfidence()
    {
        // Arrange — no OrderId, same amount, date within 2 days
        var now = DateTime.UtcNow;
        var line = CreateSettlementLine(orderId: null, netAmount: 750m, createdAt: now);
        var tx = CreateBankTransaction(
            amount: 750m,
            transactionDate: now.AddDays(1),
            description: "EFT Transfer");

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line },
            new[] { tx });

        // Assert
        result.Should().HaveCount(1);
        result[0].Confidence.Should().BeInRange(0.60m, 0.79m,
            "Amount match + date match without OrderId should yield low-medium confidence");
        result[0].Status.Should().Be(ReconciliationStatus.NeedsReview);
    }

    // ===========================================================================
    // TEST 4: NoMatch_EmptyResult
    // Amounts don't match, no OrderId → no matches
    // ===========================================================================

    [Fact]
    public void NoMatch_EmptyResult()
    {
        // Arrange — completely different amounts, no OrderId
        var line = CreateSettlementLine(orderId: null, netAmount: 1000m);
        var tx = CreateBankTransaction(
            amount: 5000m, // wildly different
            transactionDate: DateTime.UtcNow.AddDays(30), // too far apart
            description: "Unrelated payment");

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line },
            new[] { tx });

        // Assert
        result.Should().BeEmpty(
            "No match criteria met — amounts differ greatly and no OrderId match");
    }

    // ===========================================================================
    // TEST 5: DuplicateMatch_BestScoreWins
    // Multiple lines competing for same tx → highest confidence wins
    // ===========================================================================

    [Fact]
    public void DuplicateMatch_BestScoreWins()
    {
        // Arrange — 2 lines, 1 tx. Line1 has OrderId match, Line2 doesn't.
        var orderId = "ORD-99999";
        var now = DateTime.UtcNow;

        var line1 = CreateSettlementLine(orderId: orderId, netAmount: 500m, createdAt: now);
        var line2 = CreateSettlementLine(orderId: null, netAmount: 500m, createdAt: now);

        var tx = CreateBankTransaction(
            amount: 500m,
            transactionDate: now,
            description: $"Payment for {orderId}");

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line1, line2 },
            new[] { tx });

        // Assert — Only 1 match (tx can only match once), and it should be Line1 (higher score)
        result.Should().HaveCount(1);
        result[0].SettlementBatchId.Should().Be(line1.SettlementBatchId,
            "Line with OrderId match should win over line without");
        result[0].Confidence.Should().BeGreaterThan(0.80m);
    }

    // ===========================================================================
    // TEST 6: EmptyInputs_ReturnsEmpty
    // ===========================================================================

    [Fact]
    public void EmptyLines_ReturnsEmpty()
    {
        var tx = CreateBankTransaction();

        var result = _sut.Reconcile(
            _tenantId,
            Array.Empty<SettlementLine>(),
            new[] { tx });

        result.Should().BeEmpty();
    }

    [Fact]
    public void EmptyTransactions_ReturnsEmpty()
    {
        var line = CreateSettlementLine();

        var result = _sut.Reconcile(
            _tenantId,
            new[] { line },
            Array.Empty<BankTransaction>());

        result.Should().BeEmpty();
    }

    // ===========================================================================
    // TEST 7: NullInputs_ThrowsArgumentNullException
    // ===========================================================================

    [Fact]
    public void NullLines_ThrowsArgumentNullException()
    {
        var act = () => _sut.Reconcile(
            _tenantId,
            null!,
            Array.Empty<BankTransaction>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NullTransactions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Reconcile(
            _tenantId,
            Array.Empty<SettlementLine>(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ===========================================================================
    // TEST 8: MultipleMatches_EachLineAndTxUsedOnce
    // ===========================================================================

    [Fact]
    public void MultipleMatches_EachLineAndTxUsedOnce()
    {
        // Arrange — 2 lines, 2 txs, each should match once
        var now = DateTime.UtcNow;
        var orderId1 = "ORD-001";
        var orderId2 = "ORD-002";

        var line1 = CreateSettlementLine(orderId: orderId1, netAmount: 100m, createdAt: now);
        var line2 = CreateSettlementLine(orderId: orderId2, netAmount: 200m, createdAt: now);

        var tx1 = CreateBankTransaction(amount: 100m, transactionDate: now, description: $"Pay {orderId1}");
        var tx2 = CreateBankTransaction(amount: 200m, transactionDate: now, description: $"Pay {orderId2}");

        // Act
        var result = _sut.Reconcile(
            _tenantId,
            new[] { line1, line2 },
            new[] { tx1, tx2 });

        // Assert
        result.Should().HaveCount(2, "Each line should match exactly one transaction");

        var matchedTxIds = result.Select(r => r.BankTransactionId).ToList();
        matchedTxIds.Should().OnlyHaveUniqueItems("Each transaction should be used only once");

        var matchedBatchIds = result.Select(r => r.SettlementBatchId).ToList();
        matchedBatchIds.Should().OnlyHaveUniqueItems("Each line should be used only once");
    }
}
