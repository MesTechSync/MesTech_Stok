using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Dalga 14 Sprint 2 — Accounting entity coverage tests.
/// SettlementBatch, BankTransaction, JournalEntry, Counterparty, FinancialGoal, CommissionRecord, CariHesap.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AccountingEntityCoverage")]
[Trait("Phase", "Dalga14")]
public class AccountingEntityCoverageTests
{
    // ═══════════════════════════════════════════
    // SettlementBatch Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void SettlementBatch_Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var batch = SettlementBatch.Create(
            tenantId, "Trendyol",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            10000m, 1500m, 8500m);

        batch.Id.Should().NotBeEmpty();
        batch.TenantId.Should().Be(tenantId);
        batch.Platform.Should().Be("Trendyol");
        batch.TotalGross.Should().Be(10000m);
        batch.TotalCommission.Should().Be(1500m);
        batch.TotalNet.Should().Be(8500m);
        batch.Status.Should().Be(SettlementStatus.Imported);
        batch.Lines.Should().BeEmpty();
    }

    [Fact]
    public void SettlementBatch_Create_RaisesDomainEvent()
    {
        var batch = SettlementBatch.Create(
            Guid.NewGuid(), "Hepsiburada",
            DateTime.UtcNow.AddDays(-14), DateTime.UtcNow,
            5000m, 900m, 4100m);

        batch.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void SettlementBatch_Create_NullPlatform_Throws()
    {
        var act = () => SettlementBatch.Create(
            Guid.NewGuid(), null!,
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            1000m, 100m, 900m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_Create_WhitespacePlatform_Throws()
    {
        var act = () => SettlementBatch.Create(
            Guid.NewGuid(), "   ",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            1000m, 100m, 900m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SettlementBatch_AddLine_IncreasesCount()
    {
        var batch = SettlementBatch.Create(
            Guid.NewGuid(), "N11",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            1000m, 100m, 900m);

        var line = SettlementLine.Create(
            batch.TenantId, batch.Id, "ORD-001",
            1000m, 100m, 10m, 20m, 0m, 870m);

        batch.AddLine(line);

        batch.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void SettlementBatch_MarkReconciled_ChangesStatus()
    {
        var batch = SettlementBatch.Create(
            Guid.NewGuid(), "Trendyol",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            1000m, 100m, 900m);

        batch.MarkReconciled();

        batch.Status.Should().Be(SettlementStatus.Reconciled);
    }

    [Fact]
    public void SettlementBatch_MarkDisputed_ChangesStatus()
    {
        var batch = SettlementBatch.Create(
            Guid.NewGuid(), "Hepsiburada",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            5000m, 900m, 4100m);

        batch.MarkDisputed();

        batch.Status.Should().Be(SettlementStatus.Disputed);
    }

    // ═══════════════════════════════════════════
    // SettlementLine Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void SettlementLine_Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var line = SettlementLine.Create(
            tenantId, batchId, "ORD-123",
            1000m, 150m, 10m, 30m, 20m, 790m);

        line.Id.Should().NotBeEmpty();
        line.TenantId.Should().Be(tenantId);
        line.SettlementBatchId.Should().Be(batchId);
        line.OrderId.Should().Be("ORD-123");
        line.GrossAmount.Should().Be(1000m);
        line.CommissionAmount.Should().Be(150m);
        line.ServiceFee.Should().Be(10m);
        line.CargoDeduction.Should().Be(30m);
        line.RefundDeduction.Should().Be(20m);
        line.NetAmount.Should().Be(790m);
    }

    [Fact]
    public void SettlementLine_CalculateNetAmount_ReturnsCorrectValue()
    {
        var line = SettlementLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-001",
            1000m, 150m, 10m, 30m, 20m, 0m);

        // 1000 - 150 - 10 - 30 - 20 = 790
        line.CalculateNetAmount().Should().Be(790m);
    }

    [Fact]
    public void SettlementLine_Create_NullOrderId_Allowed()
    {
        var line = SettlementLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            500m, 75m, 5m, 15m, 0m, 405m);

        line.OrderId.Should().BeNull();
    }

    // ═══════════════════════════════════════════
    // BankTransaction Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void BankTransaction_Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        var tx = BankTransaction.Create(
            tenantId, bankAccountId, date,
            1500m, "Platform payment",
            "REF-001", "IDMP-001");

        tx.Id.Should().NotBeEmpty();
        tx.TenantId.Should().Be(tenantId);
        tx.BankAccountId.Should().Be(bankAccountId);
        tx.TransactionDate.Should().Be(date);
        tx.Amount.Should().Be(1500m);
        tx.Description.Should().Be("Platform payment");
        tx.ReferenceNumber.Should().Be("REF-001");
        tx.IdempotencyKey.Should().Be("IDMP-001");
        tx.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public void BankTransaction_Create_NullDescription_Throws()
    {
        var act = () => BankTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            100m, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BankTransaction_Create_WhitespaceDescription_Throws()
    {
        var act = () => BankTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            100m, "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BankTransaction_Create_NullIdempotencyKey_GeneratesOne()
    {
        var tx = BankTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            100m, "Test payment", idempotencyKey: null);

        tx.IdempotencyKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BankTransaction_MarkReconciled_SetsFlag()
    {
        var tx = BankTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            500m, "Payment");

        tx.MarkReconciled();

        tx.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public void BankTransaction_Create_NegativeAmount_Allowed()
    {
        // Negative amounts represent outgoing payments
        var tx = BankTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            -250m, "Outgoing transfer");

        tx.Amount.Should().Be(-250m);
    }

    // ═══════════════════════════════════════════
    // JournalEntry Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void JournalEntry_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var entry = JournalEntry.Create(
            tenantId,
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            "Monthly accrual",
            "JE-001");

        entry.Id.Should().NotBeEmpty();
        entry.TenantId.Should().Be(tenantId);
        entry.Description.Should().Be("Monthly accrual");
        entry.ReferenceNumber.Should().Be("JE-001");
        entry.IsPosted.Should().BeFalse();
        entry.PostedAt.Should().BeNull();
        entry.Lines.Should().BeEmpty();
    }

    [Fact]
    public void JournalEntry_Create_NullDescription_Throws()
    {
        var act = () => JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_IncreasesCount()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m, "Debit");
        entry.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void JournalEntry_AddLine_NegativeDebit_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Test");
        var act = () => entry.AddLine(Guid.NewGuid(), -100m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_NegativeCredit_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Test");
        var act = () => entry.AddLine(Guid.NewGuid(), 0m, -50m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_AddLine_BothZero_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Test");
        var act = () => entry.AddLine(Guid.NewGuid(), 0m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JournalEntry_Validate_Balanced_NoException()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Balanced");
        entry.AddLine(Guid.NewGuid(), 500m, 0m, "Debit");
        entry.AddLine(Guid.NewGuid(), 0m, 500m, "Credit");

        var act = () => entry.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntry_Validate_Imbalanced_ThrowsImbalanceException()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Imbalanced");
        entry.AddLine(Guid.NewGuid(), 500m, 0m, "Debit");
        entry.AddLine(Guid.NewGuid(), 0m, 300m, "Credit");

        var act = () => entry.Validate();
        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void JournalEntry_Validate_SingleLine_ThrowsInvalidOperation()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Single");
        entry.AddLine(Guid.NewGuid(), 100m, 100m, "Both");

        var act = () => entry.Validate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 2 lines*");
    }

    [Fact]
    public void JournalEntry_Post_ValidEntry_SetsPosted()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "To post");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 1000m);

        entry.Post();

        entry.IsPosted.Should().BeTrue();
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void JournalEntry_Post_RaisesDomainEvent()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Event test");
        entry.AddLine(Guid.NewGuid(), 200m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 200m);

        entry.Post();

        entry.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void JournalEntry_Post_AlreadyPosted_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Double post");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Post();

        var act = () => entry.Post();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already posted*");
    }

    [Fact]
    public void JournalEntry_AddLine_AfterPost_Throws()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Posted entry");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Post();

        var act = () => entry.AddLine(Guid.NewGuid(), 50m, 0m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*posted*");
    }

    // ═══════════════════════════════════════════
    // ReconciliationMatch Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationMatch_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();
        var bankTxId = Guid.NewGuid();

        var match = ReconciliationMatch.Create(
            tenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched,
            settlementId, bankTxId);

        match.Id.Should().NotBeEmpty();
        match.TenantId.Should().Be(tenantId);
        match.Confidence.Should().Be(0.95m);
        match.Status.Should().Be(ReconciliationStatus.AutoMatched);
        match.SettlementBatchId.Should().Be(settlementId);
        match.BankTransactionId.Should().Be(bankTxId);
    }

    [Fact]
    public void ReconciliationMatch_Create_NegativeConfidence_Throws()
    {
        var act = () => ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, -0.1m, ReconciliationStatus.AutoMatched);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReconciliationMatch_Create_ConfidenceOver1_Throws()
    {
        var act = () => ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 1.1m, ReconciliationStatus.AutoMatched);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReconciliationMatch_Create_AutoMatched_RaisesDomainEvent()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);
        match.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ReconciliationMatch_Create_NeedsReview_NoDomainEvent()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.80m, ReconciliationStatus.NeedsReview);
        match.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReconciliationMatch_Approve_SetsManualMatchStatus()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.75m, ReconciliationStatus.NeedsReview);

        match.Approve("admin@mestech.com");

        match.Status.Should().Be(ReconciliationStatus.ManualMatch);
        match.ReviewedBy.Should().Be("admin@mestech.com");
        match.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReconciliationMatch_Approve_NullReviewer_Throws()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.75m, ReconciliationStatus.NeedsReview);

        var act = () => match.Approve(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReconciliationMatch_Reject_SetsRejectedStatus()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.50m, ReconciliationStatus.NeedsReview);

        match.Reject("reviewer@mestech.com");

        match.Status.Should().Be(ReconciliationStatus.Rejected);
        match.ReviewedBy.Should().Be("reviewer@mestech.com");
    }

    [Fact]
    public void ReconciliationMatch_Reject_WhitespaceReviewer_Throws()
    {
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(), DateTime.UtcNow, 0.50m, ReconciliationStatus.NeedsReview);

        var act = () => match.Reject("   ");
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // Counterparty Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Counterparty_Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var cp = Counterparty.Create(
            tenantId, "DSM Grup", CounterpartyType.Platform,
            "1234567890", "555-1234", "info@dsmgrup.com",
            "Istanbul", "Trendyol");

        cp.Id.Should().NotBeEmpty();
        cp.TenantId.Should().Be(tenantId);
        cp.Name.Should().Be("DSM Grup");
        cp.CounterpartyType.Should().Be(CounterpartyType.Platform);
        cp.VKN.Should().Be("1234567890");
        cp.Phone.Should().Be("555-1234");
        cp.Email.Should().Be("info@dsmgrup.com");
        cp.Address.Should().Be("Istanbul");
        cp.Platform.Should().Be("Trendyol");
        cp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Counterparty_Create_NullName_Throws()
    {
        var act = () => Counterparty.Create(Guid.NewGuid(), null!, CounterpartyType.Customer);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Counterparty_Update_ChangesFields()
    {
        var cp = Counterparty.Create(Guid.NewGuid(), "Old Name", CounterpartyType.Supplier);

        cp.Update("New Name", "9876543210", "555-9876", "new@email.com", "Ankara", "N11");

        cp.Name.Should().Be("New Name");
        cp.VKN.Should().Be("9876543210");
        cp.Phone.Should().Be("555-9876");
        cp.Email.Should().Be("new@email.com");
        cp.Address.Should().Be("Ankara");
        cp.Platform.Should().Be("N11");
    }

    [Fact]
    public void Counterparty_Update_NullName_Throws()
    {
        var cp = Counterparty.Create(Guid.NewGuid(), "Name", CounterpartyType.Customer);
        var act = () => cp.Update(null!, null, null, null, null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Counterparty_Deactivate_SetsInactive()
    {
        var cp = Counterparty.Create(Guid.NewGuid(), "Test", CounterpartyType.Bank);
        cp.Deactivate();
        cp.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Counterparty_Activate_SetsActive()
    {
        var cp = Counterparty.Create(Guid.NewGuid(), "Test", CounterpartyType.Carrier);
        cp.Deactivate();
        cp.Activate();
        cp.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // FinancialGoal Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void FinancialGoal_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var goal = FinancialGoal.Create(
            tenantId, "Revenue Target Q1",
            100000m,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        goal.Id.Should().NotBeEmpty();
        goal.TenantId.Should().Be(tenantId);
        goal.Title.Should().Be("Revenue Target Q1");
        goal.TargetAmount.Should().Be(100000m);
        goal.CurrentAmount.Should().Be(0m);
        goal.IsAchieved.Should().BeFalse();
    }

    [Fact]
    public void FinancialGoal_Create_NullTitle_Throws()
    {
        var act = () => FinancialGoal.Create(
            Guid.NewGuid(), null!, 1000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FinancialGoal_Create_ZeroTarget_Throws()
    {
        var act = () => FinancialGoal.Create(
            Guid.NewGuid(), "Goal", 0m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FinancialGoal_Create_NegativeTarget_Throws()
    {
        var act = () => FinancialGoal.Create(
            Guid.NewGuid(), "Goal", -1000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FinancialGoal_Create_EndBeforeStart_Throws()
    {
        var act = () => FinancialGoal.Create(
            Guid.NewGuid(), "Goal", 1000m,
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FinancialGoal_UpdateProgress_BelowTarget_NotAchieved()
    {
        var goal = FinancialGoal.Create(
            Guid.NewGuid(), "Test", 10000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        goal.UpdateProgress(5000m);

        goal.CurrentAmount.Should().Be(5000m);
        goal.IsAchieved.Should().BeFalse();
    }

    [Fact]
    public void FinancialGoal_UpdateProgress_ExactTarget_Achieved()
    {
        var goal = FinancialGoal.Create(
            Guid.NewGuid(), "Test", 10000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        goal.UpdateProgress(10000m);

        goal.CurrentAmount.Should().Be(10000m);
        goal.IsAchieved.Should().BeTrue();
    }

    [Fact]
    public void FinancialGoal_UpdateProgress_AboveTarget_Achieved()
    {
        var goal = FinancialGoal.Create(
            Guid.NewGuid(), "Test", 10000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        goal.UpdateProgress(15000m);

        goal.IsAchieved.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // CommissionRecord Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void CommissionRecord_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var record = CommissionRecord.Create(
            tenantId, "Trendyol",
            1000m, 0.15m, 150m, 5m,
            "ORD-001", "Electronics");

        record.Id.Should().NotBeEmpty();
        record.TenantId.Should().Be(tenantId);
        record.Platform.Should().Be("Trendyol");
        record.GrossAmount.Should().Be(1000m);
        record.CommissionRate.Should().Be(0.15m);
        record.CommissionAmount.Should().Be(150m);
        record.ServiceFee.Should().Be(5m);
        record.OrderId.Should().Be("ORD-001");
        record.Category.Should().Be("Electronics");
    }

    [Fact]
    public void CommissionRecord_Create_NullPlatform_Throws()
    {
        var act = () => CommissionRecord.Create(
            Guid.NewGuid(), null!, 1000m, 0.15m, 150m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommissionRecord_Create_NegativeGross_Throws()
    {
        var act = () => CommissionRecord.Create(
            Guid.NewGuid(), "Trendyol", -100m, 0.15m, 0m, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CommissionRecord_Create_NegativeRate_Throws()
    {
        var act = () => CommissionRecord.Create(
            Guid.NewGuid(), "Trendyol", 1000m, -0.05m, 0m, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CommissionRecord_GetNetAmount_ReturnsCorrect()
    {
        var record = CommissionRecord.Create(
            Guid.NewGuid(), "Trendyol",
            1000m, 0.15m, 150m, 10m);

        // 1000 - 150 - 10 = 840
        record.GetNetAmount().Should().Be(840m);
    }

    // ═══════════════════════════════════════════
    // CariHesap Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void CariHesap_GetBakiye_EmptyHareketler_ReturnsZero()
    {
        var hesap = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Customer",
            Type = CariHesapType.Musteri
        };

        hesap.GetBakiye().Should().Be(0m);
    }

    [Fact]
    public void CariHesap_AddHareket_NullHareket_Throws()
    {
        var hesap = new CariHesap { Name = "Test" };
        var act = () => hesap.AddHareket(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CariHesap_GetBakiye_BorcMinusAlacak()
    {
        var hesap = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Customer",
            Type = CariHesapType.Musteri
        };

        hesap.AddHareket(new CariHareket
        {
            TenantId = hesap.TenantId,
            CariHesapId = hesap.Id,
            Amount = 1000m,
            Direction = CariDirection.Borc,
            Description = "Invoice"
        });

        hesap.AddHareket(new CariHareket
        {
            TenantId = hesap.TenantId,
            CariHesapId = hesap.Id,
            Amount = 400m,
            Direction = CariDirection.Alacak,
            Description = "Payment"
        });

        // 1000 - 400 = 600
        hesap.GetBakiye().Should().Be(600m);
    }

    [Fact]
    public void CariHesap_GetBakiye_NegativeWhenAlacakExceedsBorc()
    {
        var hesap = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Supplier",
            Type = CariHesapType.Tedarikci
        };

        hesap.AddHareket(new CariHareket
        {
            TenantId = hesap.TenantId,
            CariHesapId = hesap.Id,
            Amount = 200m,
            Direction = CariDirection.Borc,
            Description = "Purchase"
        });

        hesap.AddHareket(new CariHareket
        {
            TenantId = hesap.TenantId,
            CariHesapId = hesap.Id,
            Amount = 500m,
            Direction = CariDirection.Alacak,
            Description = "Overpayment"
        });

        // 200 - 500 = -300
        hesap.GetBakiye().Should().Be(-300m);
    }

    // ═══════════════════════════════════════════
    // JournalEntryImbalanceException Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void JournalEntryImbalanceException_ContainsDebitAndCredit()
    {
        var ex = new JournalEntryImbalanceException(1500m, 1000m);
        ex.TotalDebit.Should().Be(1500m);
        ex.TotalCredit.Should().Be(1000m);
        ex.Message.Should().Contain("imbalanced");
        ex.Message.Should().Contain("debit");
        ex.Message.Should().Contain("credit");
    }
}
