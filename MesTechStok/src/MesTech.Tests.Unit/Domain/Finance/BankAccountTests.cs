using FluentAssertions;
using MesTech.Domain.Entities.Finance;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Finance;

public class BankAccountTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSetBalanceToZero()
    {
        var account = BankAccount.Create(_tenantId, "İş Bankası TRY", "TRY");
        account.Balance.Should().Be(0m);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => BankAccount.Create(_tenantId, "", "TRY");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdjustBalance_Positive_ShouldIncreaseBalance()
    {
        var account = BankAccount.Create(_tenantId, "Test", "TRY");
        account.AdjustBalance(1000m);
        account.Balance.Should().Be(1000m);
    }

    [Fact]
    public void AdjustBalance_Negative_ShouldDecreaseBalance()
    {
        var account = BankAccount.Create(_tenantId, "Test", "TRY");
        account.AdjustBalance(500m);
        account.AdjustBalance(-200m);
        account.Balance.Should().Be(300m);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var account = BankAccount.Create(_tenantId, "Test", "TRY");
        account.Deactivate();
        account.IsActive.Should().BeFalse();
    }
}
