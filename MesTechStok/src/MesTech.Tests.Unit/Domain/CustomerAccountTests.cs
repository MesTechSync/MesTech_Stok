using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// CustomerAccount entity unit tests — balance calculation + return recording.
/// 4 tests: zero balance, debit increases, credit decreases, multiple tx.
/// Demir Kural 7: Cari hesap bakiye tutarlı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "CustomerAccount")]
public class CustomerAccountTests
{
    private static CustomerAccount CreateAccount()
    {
        return new CustomerAccount
        {
            TenantId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            AccountCode = "MUS-001",
            CustomerName = "Test Musteri A.S.",
            Currency = "TRY"
        };
    }

    // ════ 1. New account — balance is zero ════

    [Fact]
    public void NewAccount_Balance_IsZero()
    {
        // Act
        var account = CreateAccount();

        // Assert
        account.Balance.Should().Be(0m);
        account.Transactions.Should().BeEmpty();
        account.IsActive.Should().BeTrue();
    }

    // ════ 2. Debit transaction increases balance ════

    [Fact]
    public void AddDebitTransaction_IncreasesBalance()
    {
        // Arrange
        var account = CreateAccount();

        // Act — sale invoice (debit = customer owes us)
        account.RecordSale(
            invoiceId: Guid.NewGuid(),
            orderId: Guid.NewGuid(),
            amount: 1200m,
            invoiceNumber: "INV-001",
            platform: PlatformType.Trendyol);

        // Assert
        account.Balance.Should().Be(1200m);
        account.Transactions.Should().HaveCount(1);
        var tx = account.Transactions.First();
        tx.Type.Should().Be(TransactionType.SalesInvoice);
        tx.DebitAmount.Should().Be(1200m);
        tx.CreditAmount.Should().Be(0m);
    }

    // ════ 3. Credit transaction (return) decreases balance ════

    [Fact]
    public void RecordReturn_DecreasesBalance()
    {
        // Arrange
        var account = CreateAccount();
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1200m, "INV-001");

        // Act — return recorded (credit = we owe customer)
        var returnTx = account.RecordReturn(
            returnRequestId: Guid.NewGuid(),
            amount: 300m,
            platform: PlatformType.Trendyol);

        // Assert
        account.Balance.Should().Be(900m); // 1200 - 300
        returnTx.Type.Should().Be(TransactionType.SalesReturn);
        returnTx.CreditAmount.Should().Be(300m);
        returnTx.DebitAmount.Should().Be(0m);
        returnTx.ReturnRequestId.Should().NotBeNull();
    }

    // ════ 4. Multiple transactions — balance calculated correctly ════

    [Fact]
    public void MultipleTransactions_BalanceCalculatedCorrectly()
    {
        // Arrange
        var account = CreateAccount();

        // Act — 2 sales, 1 collection, 1 return
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "INV-001");
        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 500m, "INV-002");
        account.RecordCollection(800m, "TAH-001");
        account.RecordReturn(Guid.NewGuid(), 200m);

        // Assert — 1000 + 500 - 800 - 200 = 500
        account.Balance.Should().Be(500m);
        account.Transactions.Should().HaveCount(4);
        account.HasExceededCreditLimit.Should().BeFalse(); // CreditLimit=0, no limit
    }
}
