using FluentAssertions;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Finance;

/// <summary>
/// GLTransaction entity unit testleri.
/// DEV 5 — H27-5.6 (emirname Task 5.6 uyarlanmis gercek entity'ye gore)
/// Not: Gercek entity TransactionType degil GLTransactionType kullanir.
///      SetExchangeRate ve AmountInTRY H27 entity'sinde yok — Reconcile test edildi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Finance")]
public class GLTransactionTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_Income_ShouldSetTypeToIncome()
    {
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Income, 1000m, "Trendyol satis", _userId);

        tx.Type.Should().Be(GLTransactionType.Income);
        tx.Amount.Should().Be(1000m);
        tx.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyDescription_ShouldThrow()
    {
        var act = () => GLTransaction.Create(_tenantId, GLTransactionType.Expense, 500m, "", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceDescription_ShouldThrow()
    {
        var act = () => GLTransaction.Create(_tenantId, GLTransactionType.Expense, 500m, "   ", _userId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reconcile_ShouldMarkAsReconciled()
    {
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Income, 1000m, "Test", _userId);
        tx.Reconcile();

        tx.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public void Create_WithBankAccountId_ShouldSetBankAccountId()
    {
        var bankId = Guid.NewGuid();
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Transfer, 500m, "Transfer", _userId,
            bankAccountId: bankId);

        tx.BankAccountId.Should().Be(bankId);
    }

    [Fact]
    public void Create_WithOrderId_ShouldSetOrderId()
    {
        var orderId = Guid.NewGuid();
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Income, 2500m, "Siparis geliri", _userId,
            orderId: orderId);

        tx.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Create_WithExpenseId_ShouldSetExpenseId()
    {
        var expenseId = Guid.NewGuid();
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Expense, 300m, "Gider karsiligi", _userId,
            expenseId: expenseId);

        tx.ExpenseId.Should().Be(expenseId);
    }

    [Fact]
    public void Create_WithCurrency_ShouldPreserveCurrency()
    {
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Income, 100m, "USD gelir", _userId,
            currency: "USD");

        tx.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_ShouldSetCreatedByUserId()
    {
        var tx = GLTransaction.Create(_tenantId, GLTransactionType.Income, 500m, "Test", _userId);

        tx.CreatedByUserId.Should().Be(_userId);
    }

    [Theory]
    [InlineData(GLTransactionType.Income)]
    [InlineData(GLTransactionType.Expense)]
    [InlineData(GLTransactionType.Transfer)]
    [InlineData(GLTransactionType.Refund)]
    public void Create_AllTransactionTypes_ShouldSucceed(GLTransactionType txType)
    {
        var tx = GLTransaction.Create(_tenantId, txType, 100m, "Test", _userId);

        tx.Type.Should().Be(txType);
    }
}
