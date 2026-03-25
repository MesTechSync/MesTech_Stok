using FluentAssertions;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Finance domain entity tests: Expense, Income, BankAccount, CashRegister,
/// CashTransaction, FinanceExpense, PaymentTransaction, AccountTransaction,
/// GLTransaction, PriceHistory.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "FinanceEntities")]
[Trait("Phase", "Dalga15")]
public class FinanceEntityTests
{
    // ═══════════════════════════════════════════
    // Expense
    // ═══════════════════════════════════════════

    [Fact]
    public void Expense_Creation_SetsDefaults()
    {
        var expense = new Expense();

        expense.Id.Should().NotBe(Guid.Empty);
        expense.Currency.Should().Be("TRY");
        expense.PaymentStatus.Should().Be(PaymentStatus.Pending);
        expense.Description.Should().BeEmpty();
    }

    [Fact]
    public void Expense_SetAmount_PositiveValue_SetsAmount()
    {
        var expense = new Expense();
        expense.SetAmount(150m);
        expense.Amount.Should().Be(150m);
    }

    [Fact]
    public void Expense_SetAmount_ZeroOrNegative_Throws()
    {
        var expense = new Expense();

        var act1 = () => expense.SetAmount(0m);
        var act2 = () => expense.SetAmount(-5m);

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Expense_MarkAsProcessing_FromPending_TransitionsCorrectly()
    {
        var expense = new Expense();
        expense.MarkAsProcessing();
        expense.PaymentStatus.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void Expense_MarkAsProcessing_FromCompleted_Throws()
    {
        var expense = new Expense();
        expense.MarkAsProcessing();
        expense.MarkAsCompleted();

        var act = () => expense.MarkAsProcessing();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expense_MarkAsCompleted_FromPending_Succeeds()
    {
        var expense = new Expense();
        expense.MarkAsCompleted();
        expense.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void Expense_MarkAsCompleted_FromProcessing_Succeeds()
    {
        var expense = new Expense();
        expense.MarkAsProcessing();
        expense.MarkAsCompleted();
        expense.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void Expense_Cancel_FromPending_Succeeds()
    {
        var expense = new Expense();
        expense.Cancel();
        expense.PaymentStatus.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void Expense_Cancel_FromCompleted_Throws()
    {
        var expense = new Expense();
        expense.MarkAsCompleted();

        var act = () => expense.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    // ═══════════════════════════════════════════
    // Income
    // ═══════════════════════════════════════════

    [Fact]
    public void Income_Creation_SetsDefaults()
    {
        var income = new Income();

        income.Id.Should().NotBe(Guid.Empty);
        income.Currency.Should().Be("TRY");
        income.Source.Should().Be(IncomeSource.Manual);
        income.Description.Should().BeEmpty();
    }

    [Fact]
    public void Income_SetAmount_ValidValue_SetsAmount()
    {
        var income = new Income();
        income.SetAmount(500m);
        income.Amount.Should().Be(500m);
    }

    [Fact]
    public void Income_SetAmount_Zero_Succeeds()
    {
        var income = new Income();
        income.SetAmount(0m);
        income.Amount.Should().Be(0m);
    }

    [Fact]
    public void Income_SetAmount_Negative_Throws()
    {
        var income = new Income();
        var act = () => income.SetAmount(-1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Income_SetDeductions_ValidValues_SetsCorrectly()
    {
        var income = new Income();
        income.SetAmount(1000m);
        income.SetDeductions(100m, 50m);

        income.CommissionAmount.Should().Be(100m);
        income.ShippingCost.Should().Be(50m);
    }

    [Fact]
    public void Income_SetDeductions_NegativeCommission_Throws()
    {
        var income = new Income();
        var act = () => income.SetDeductions(-1m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Income_SetDeductions_NegativeShipping_Throws()
    {
        var income = new Income();
        var act = () => income.SetDeductions(0m, -1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Income_NetAmount_ComputedCorrectly()
    {
        var income = new Income();
        income.SetAmount(1000m);
        income.SetDeductions(100m, 50m);

        income.NetAmount.Should().Be(850m);
    }

    // ═══════════════════════════════════════════
    // BankAccount
    // ═══════════════════════════════════════════

    [Fact]
    public void BankAccount_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var ba = BankAccount.Create(tenantId, "Ana Hesap", "USD", "Garanti", "TR123456");

        ba.Id.Should().NotBe(Guid.Empty);
        ba.TenantId.Should().Be(tenantId);
        ba.AccountName.Should().Be("Ana Hesap");
        ba.Currency.Should().Be("USD");
        ba.BankName.Should().Be("Garanti");
        ba.IBAN.Should().Be("TR123456");
        ba.Balance.Should().Be(0m);
        ba.IsActive.Should().BeTrue();
        ba.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void BankAccount_Create_EmptyName_Throws()
    {
        var act = () => BankAccount.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BankAccount_Create_WithDefault_SetsFlag()
    {
        var ba = BankAccount.Create(Guid.NewGuid(), "Hesap", isDefault: true);
        ba.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void BankAccount_AdjustBalance_UpdatesBalance()
    {
        var ba = BankAccount.Create(Guid.NewGuid(), "Hesap");
        ba.AdjustBalance(500m);
        ba.Balance.Should().Be(500m);

        ba.AdjustBalance(-200m);
        ba.Balance.Should().Be(300m);
    }

    [Fact]
    public void BankAccount_SetAsDefault_SetsFlag()
    {
        var ba = BankAccount.Create(Guid.NewGuid(), "Hesap");
        ba.SetAsDefault();
        ba.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void BankAccount_Deactivate_SetsInactive()
    {
        var ba = BankAccount.Create(Guid.NewGuid(), "Hesap");
        ba.Deactivate();
        ba.IsActive.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // CashRegister
    // ═══════════════════════════════════════════

    [Fact]
    public void CashRegister_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var cr = CashRegister.Create(tenantId, "Ana Kasa", "TRY", true, 1000m);

        cr.Id.Should().NotBe(Guid.Empty);
        cr.TenantId.Should().Be(tenantId);
        cr.Name.Should().Be("Ana Kasa");
        cr.CurrencyCode.Should().Be("TRY");
        cr.IsDefault.Should().BeTrue();
        cr.Balance.Should().Be(1000m);
        cr.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CashRegister_Create_EmptyName_Throws()
    {
        var act = () => CashRegister.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CashRegister_RecordIncome_IncreasesBalanceAndAddsTx()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa", "TRY", false, 100m);

        var tx = cr.RecordIncome(200m, "Satis geliri", "Satis");

        cr.Balance.Should().Be(300m);
        cr.Transactions.Should().HaveCount(1);
        tx.Amount.Should().Be(200m);
        tx.Type.Should().Be(CashTransactionType.Income);
    }

    [Fact]
    public void CashRegister_RecordIncome_ZeroAmount_Throws()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa");
        var act = () => cr.RecordIncome(0m, "test");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CashRegister_RecordExpense_DecreasesBalanceAndAddsTx()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa", "TRY", false, 500m);

        var tx = cr.RecordExpense(150m, "Kira", "Gider");

        cr.Balance.Should().Be(350m);
        cr.Transactions.Should().HaveCount(1);
        tx.Type.Should().Be(CashTransactionType.Expense);
    }

    [Fact]
    public void CashRegister_RecordExpense_ZeroAmount_Throws()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa");
        var act = () => cr.RecordExpense(0m, "test");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CashRegister_RecordIncome_RaisesDomainEvent()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa");
        cr.RecordIncome(100m, "test");
        cr.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void CashRegister_DeactivateAndActivate_TogglesState()
    {
        var cr = CashRegister.Create(Guid.NewGuid(), "Kasa");
        cr.Deactivate();
        cr.IsActive.Should().BeFalse();
        cr.Activate();
        cr.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // CashTransaction
    // ═══════════════════════════════════════════

    [Fact]
    public void CashTransaction_Create_SetsProperties()
    {
        var cashRegisterId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tx = CashTransaction.Create(cashRegisterId, tenantId, CashTransactionType.Income, 250m, "Satis geliri", "Satis");

        tx.Id.Should().NotBe(Guid.Empty);
        tx.CashRegisterId.Should().Be(cashRegisterId);
        tx.TenantId.Should().Be(tenantId);
        tx.Type.Should().Be(CashTransactionType.Income);
        tx.Amount.Should().Be(250m);
        tx.Description.Should().Be("Satis geliri");
        tx.Category.Should().Be("Satis");
    }

    [Fact]
    public void CashTransaction_Create_EmptyDescription_Throws()
    {
        var act = () => CashTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), CashTransactionType.Income, 100m, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CashTransaction_Create_ZeroAmount_Throws()
    {
        var act = () => CashTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), CashTransactionType.Income, 0m, "test");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CashTransaction_Create_NegativeAmount_Throws()
    {
        var act = () => CashTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), CashTransactionType.Expense, -10m, "test");
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // FinanceExpense
    // ═══════════════════════════════════════════

    [Fact]
    public void FinanceExpense_Create_SetsDefaults()
    {
        var tenantId = Guid.NewGuid();
        var fe = FinanceExpense.Create(tenantId, "Ofis Kirasi", 5000m, ExpenseCategory.Accommodation, DateTime.UtcNow);

        fe.Id.Should().NotBe(Guid.Empty);
        fe.TenantId.Should().Be(tenantId);
        fe.Title.Should().Be("Ofis Kirasi");
        fe.Amount.Should().Be(5000m);
        fe.Status.Should().Be(ExpenseStatus.Draft);
    }

    [Fact]
    public void FinanceExpense_Create_EmptyTitle_Throws()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FinanceExpense_Create_ZeroAmount_Throws()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "Test", 0m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FinanceExpense_Submit_FromDraft_Succeeds()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        fe.Submit();
        fe.Status.Should().Be(ExpenseStatus.Submitted);
    }

    [Fact]
    public void FinanceExpense_Submit_FromSubmitted_Throws()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        fe.Submit();

        var act = () => fe.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FinanceExpense_Approve_FromSubmitted_Succeeds()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        fe.Submit();

        var approverId = Guid.NewGuid();
        fe.Approve(approverId);

        fe.Status.Should().Be(ExpenseStatus.Approved);
        fe.ApprovedByUserId.Should().Be(approverId);
        fe.ApprovedAt.Should().NotBeNull();
        fe.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void FinanceExpense_Approve_FromDraft_Throws()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        var act = () => fe.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FinanceExpense_Reject_FromSubmitted_Succeeds()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        fe.Submit();
        fe.Reject("Butce asimi");

        fe.Status.Should().Be(ExpenseStatus.Rejected);
        fe.Notes.Should().Contain("Red:");
    }

    [Fact]
    public void FinanceExpense_MarkAsPaid_FromApproved_Succeeds()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        fe.Submit();
        fe.Approve(Guid.NewGuid());
        fe.ClearDomainEvents();

        var bankId = Guid.NewGuid();
        fe.MarkAsPaid(bankId);

        fe.Status.Should().Be(ExpenseStatus.Paid);
        fe.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void FinanceExpense_MarkAsPaid_FromDraft_Throws()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        var act = () => fe.MarkAsPaid(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FinanceExpense_AttachDocument_SetsDocumentId()
    {
        var fe = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Accommodation, DateTime.UtcNow);
        var docId = Guid.NewGuid();
        fe.AttachDocument(docId);
        fe.DocumentId.Should().Be(docId);
    }

    // ═══════════════════════════════════════════
    // PaymentTransaction
    // ═══════════════════════════════════════════

    [Fact]
    public void PaymentTransaction_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var pt = PaymentTransaction.Create(tenantId, orderId, PaymentProviderType.PayTRDirect, 250m, "TRY", 3);

        pt.Id.Should().NotBe(Guid.Empty);
        pt.TenantId.Should().Be(tenantId);
        pt.OrderId.Should().Be(orderId);
        pt.Amount.Should().Be(250m);
        pt.InstallmentCount.Should().Be(3);
        pt.Status.Should().Be(PaymentTransactionStatus.Pending);
    }

    [Fact]
    public void PaymentTransaction_Create_EmptyTenantId_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.Empty, Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PaymentTransaction_Create_EmptyOrderId_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), Guid.Empty, PaymentProviderType.PayTRDirect, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PaymentTransaction_Create_ZeroAmount_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PaymentTransaction_Create_ZeroInstallment_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m, installmentCount: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PaymentTransaction_MarkCompleted_FromPending_Succeeds()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        pt.MarkCompleted("TX-123");

        pt.Status.Should().Be(PaymentTransactionStatus.Completed);
        pt.TransactionId.Should().Be("TX-123");
        pt.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void PaymentTransaction_MarkCompleted_EmptyTransactionId_Throws()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        var act = () => pt.MarkCompleted("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PaymentTransaction_MarkFailed_FromPending_Succeeds()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        pt.MarkFailed();
        pt.Status.Should().Be(PaymentTransactionStatus.Failed);
    }

    [Fact]
    public void PaymentTransaction_MarkFailed_FromCompleted_Throws()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        pt.MarkCompleted("TX-1");

        var act = () => pt.MarkFailed();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PaymentTransaction_MarkRefunded_FromCompleted_Succeeds()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        pt.MarkCompleted("TX-1");
        pt.MarkRefunded();

        pt.Status.Should().Be(PaymentTransactionStatus.Refunded);
        pt.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public void PaymentTransaction_MarkRefunded_FromPending_Throws()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        var act = () => pt.MarkRefunded();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PaymentTransaction_ToString_ReturnsFormattedString()
    {
        var pt = PaymentTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProviderType.PayTRDirect, 100m);
        pt.ToString().Should().Contain("PayTR");
    }

    // ═══════════════════════════════════════════
    // AccountTransaction
    // ═══════════════════════════════════════════

    [Fact]
    public void AccountTransaction_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var at = AccountTransaction.Create(tenantId, accountId, TransactionType.SalesInvoice, 500m, 0m, "Satis");

        at.Id.Should().NotBe(Guid.Empty);
        at.TenantId.Should().Be(tenantId);
        at.AccountId.Should().Be(accountId);
        at.DebitAmount.Should().Be(500m);
        at.CreditAmount.Should().Be(0m);
        at.Currency.Should().Be("TRY");
    }

    [Fact]
    public void AccountTransaction_Create_NegativeDebit_Throws()
    {
        var act = () => AccountTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), TransactionType.SalesInvoice, -1m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AccountTransaction_Create_NegativeCredit_Throws()
    {
        var act = () => AccountTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), TransactionType.SalesInvoice, 0m, -1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AccountTransaction_Create_BothZero_Throws()
    {
        var act = () => AccountTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), TransactionType.SalesInvoice, 0m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AccountTransaction_NetAmount_ComputedCorrectly()
    {
        var at = AccountTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), TransactionType.SalesInvoice, 500m, 100m);
        at.NetAmount.Should().Be(400m);
    }

    [Fact]
    public void AccountTransaction_ToString_ReturnsFormattedString()
    {
        var at = AccountTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), TransactionType.Collection, 0m, 300m, documentNumber: "COL-001");
        at.ToString().Should().Contain("COL-001");
    }

    // ═══════════════════════════════════════════
    // GLTransaction
    // ═══════════════════════════════════════════

    [Fact]
    public void GLTransaction_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gl = GLTransaction.Create(tenantId, GLTransactionType.Income, 1000m, "Satis kaydı", userId, "TRY");

        gl.Id.Should().NotBe(Guid.Empty);
        gl.TenantId.Should().Be(tenantId);
        gl.Amount.Should().Be(1000m);
        gl.Description.Should().Be("Satis kaydı");
        gl.CreatedByUserId.Should().Be(userId);
        gl.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public void GLTransaction_Create_EmptyDescription_Throws()
    {
        var act = () => GLTransaction.Create(Guid.NewGuid(), GLTransactionType.Income, 100m, "", Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GLTransaction_Reconcile_SetsFlag()
    {
        var gl = GLTransaction.Create(Guid.NewGuid(), GLTransactionType.Income, 100m, "Test", Guid.NewGuid());
        gl.Reconcile();
        gl.IsReconciled.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // PriceHistory
    // ═══════════════════════════════════════════

    [Fact]
    public void PriceHistory_Creation_SetsDefaults()
    {
        var ph = new PriceHistory();

        ph.Id.Should().NotBe(Guid.Empty);
        ph.Currency.Should().Be("TRY");
    }

    [Fact]
    public void PriceHistory_PriceChangePercent_ComputedCorrectly()
    {
        var ph = new PriceHistory { OldPrice = 100m, NewPrice = 120m };
        ph.PriceChangePercent.Should().Be(20m);
    }

    [Fact]
    public void PriceHistory_PriceChangePercent_Decrease_Negative()
    {
        var ph = new PriceHistory { OldPrice = 200m, NewPrice = 150m };
        ph.PriceChangePercent.Should().Be(-25m);
    }

    [Fact]
    public void PriceHistory_PriceChangePercent_OldPriceZero_ReturnsZero()
    {
        var ph = new PriceHistory { OldPrice = 0m, NewPrice = 100m };
        ph.PriceChangePercent.Should().Be(0m);
    }
}
