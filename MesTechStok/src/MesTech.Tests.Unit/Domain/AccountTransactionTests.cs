using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "AccountTransaction")]
[Trait("Phase", "Dalga5")]
public class AccountTransactionTests
{
    [Fact]
    public void NetAmount_DebitMinusCredit()
    {
        var tx = new AccountTransaction { DebitAmount = 100m, CreditAmount = 40m };
        tx.NetAmount.Should().Be(60m);
    }

    [Fact]
    public void NetAmount_ZeroBalance_WhenEqual()
    {
        var tx = new AccountTransaction { DebitAmount = 50m, CreditAmount = 50m };
        tx.NetAmount.Should().Be(0m);
    }

    [Fact]
    public void NetAmount_Negative_WhenCreditExceedsDebit()
    {
        var tx = new AccountTransaction { DebitAmount = 0m, CreditAmount = 200m };
        tx.NetAmount.Should().Be(-200m);
    }

    [Fact]
    public void ToString_ContainsTypeAmountsAndDocumentNumber()
    {
        var tx = new AccountTransaction
        {
            Type = TransactionType.Payment,
            DebitAmount = 100m,
            CreditAmount = 0m,
            DocumentNumber = "ODE-001"
        };
        var str = tx.ToString();
        str.Should().Contain("Payment");
        str.Should().Contain("100");
        str.Should().Contain("ODE-001");
    }

    [Fact]
    public void DefaultCurrency_IsTRY()
    {
        var tx = new AccountTransaction();
        tx.Currency.Should().Be("TRY");
        tx.TransactionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
