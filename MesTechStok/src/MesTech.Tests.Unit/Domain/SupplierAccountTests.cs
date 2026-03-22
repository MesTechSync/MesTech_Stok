using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "SupplierAccount")]
[Trait("Phase", "Dalga5")]
public class SupplierAccountTests
{
    private static SupplierAccount MakeAccount()
    {
        var account = new SupplierAccount
        {
            TenantId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            AccountCode = "SUP-001",
            SupplierName = "Test Tedarikçi",
            Currency = "TRY",
        };
        account.Activate();
        return account;
    }

    [Fact]
    public void Balance_NoTransactions_ReturnsZero()
    {
        var account = MakeAccount();
        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void RecordPurchase_AddsCreditTransaction_BalanceNegative()
    {
        var account = MakeAccount();
        var invoiceId = Guid.NewGuid();

        var tx = account.RecordPurchase(invoiceId, 500m, "FAT-001");

        tx.Type.Should().Be(TransactionType.PurchaseInvoice);
        tx.CreditAmount.Should().Be(500m);
        tx.DebitAmount.Should().Be(0m);
        tx.InvoiceId.Should().Be(invoiceId);
        tx.DocumentNumber.Should().Be("FAT-001");
        account.Balance.Should().Be(-500m); // we owe supplier
    }

    [Fact]
    public void RecordPayment_AddsDebitTransaction_ReducesDebt()
    {
        var account = MakeAccount();
        account.RecordPurchase(Guid.NewGuid(), 500m, "FAT-001");

        var tx = account.RecordPayment(300m, "ODE-001");

        tx.Type.Should().Be(TransactionType.Payment);
        tx.DebitAmount.Should().Be(300m);
        tx.CreditAmount.Should().Be(0m);
        account.Balance.Should().Be(-200m); // still owe 200
    }

    [Fact]
    public void RecordPayment_WithDueDate_SetsOnTransaction()
    {
        var account = MakeAccount();
        var dueDate = DateTime.UtcNow.AddDays(30);

        var tx = account.RecordPayment(100m, "ODE-002", dueDate);

        tx.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void RecordPurchaseReturn_AddsDebitTransaction_ReducesDebt()
    {
        var account = MakeAccount();
        account.RecordPurchase(Guid.NewGuid(), 500m, "FAT-001");

        var returnId = Guid.NewGuid();
        var tx = account.RecordPurchaseReturn(returnId, 100m);

        tx.Type.Should().Be(TransactionType.PurchaseReturn);
        tx.DebitAmount.Should().Be(100m);
        tx.CreditAmount.Should().Be(0m);
        tx.ReturnRequestId.Should().Be(returnId);
        account.Balance.Should().Be(-400m);
    }

    [Fact]
    public void AddTransaction_SetsAccountId()
    {
        var account = MakeAccount();
        var tx = new AccountTransaction
        {
            TenantId = account.TenantId,
            Type = TransactionType.Payment,
            DebitAmount = 100m,
            CreditAmount = 0m
        };

        account.AddTransaction(tx);

        tx.AccountId.Should().Be(account.Id);
        account.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void OverdueBalance_FiltersToOverdueTransactions()
    {
        var account = MakeAccount();
        var asOf = DateTime.UtcNow;

        // Overdue (past due)
        account.AddTransaction(new AccountTransaction
        {
            TenantId = account.TenantId,
            Type = TransactionType.Payment,
            DebitAmount = 100m,
            CreditAmount = 0m,
            DueDate = asOf.AddDays(-5)
        });

        // Not yet due
        account.AddTransaction(new AccountTransaction
        {
            TenantId = account.TenantId,
            Type = TransactionType.Payment,
            DebitAmount = 200m,
            CreditAmount = 0m,
            DueDate = asOf.AddDays(5)
        });

        account.OverdueBalance(asOf).Should().Be(100m);
    }

    [Fact]
    public void OverdueBalance_NoOverdue_ReturnsZero()
    {
        var account = MakeAccount();
        account.OverdueBalance(DateTime.UtcNow).Should().Be(0m);
    }

    [Fact]
    public void Transactions_IsReadOnlyCollection()
    {
        var account = MakeAccount();
        account.Transactions.Should().BeAssignableTo<IReadOnlyCollection<AccountTransaction>>();
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ContainsCodeNameAndBalance()
    {
        var account = MakeAccount();
        account.RecordPurchase(Guid.NewGuid(), 250m, "FAT-X");

        var str = account.ToString();
        str.Should().Contain("SUP-001");
        str.Should().Contain("Test Tedarikçi");
        str.Should().Contain("250");
    }
}
