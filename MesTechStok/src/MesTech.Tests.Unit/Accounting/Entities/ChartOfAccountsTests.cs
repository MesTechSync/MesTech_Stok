using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class ChartOfAccountsTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        coa.Should().NotBeNull();
        coa.Code.Should().Be("100");
        coa.Name.Should().Be("Kasa");
        coa.AccountType.Should().Be(AccountType.Asset);
    }

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        coa.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetDefaultLevel()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        coa.Level.Should().Be(1);
    }

    [Fact]
    public void Create_WithCustomLevel_ShouldSetLevel()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100.01", "Merkez Kasa", AccountType.Asset, level: 2);

        coa.Level.Should().Be(2);
    }

    [Fact]
    public void Create_WithParentId_ShouldSetParentId()
    {
        var parentId = Guid.NewGuid();
        var coa = ChartOfAccounts.Create(
            _tenantId, "100.01", "Alt Hesap", AccountType.Asset, parentId);

        coa.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => ChartOfAccounts.Create(
            _tenantId, "", "Kasa", AccountType.Asset);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ChartOfAccounts.Create(
            _tenantId, "100", "", AccountType.Asset);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        coa.Deactivate();

        coa.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);
        coa.Deactivate();

        coa.Activate();

        coa.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_ShouldChangeName()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        coa.UpdateName("Merkez Kasa");

        coa.Name.Should().Be("Merkez Kasa");
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrow()
    {
        var coa = ChartOfAccounts.Create(
            _tenantId, "100", "Kasa", AccountType.Asset);

        var act = () => coa.UpdateName("");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(AccountType.Asset)]
    [InlineData(AccountType.Liability)]
    [InlineData(AccountType.Equity)]
    [InlineData(AccountType.Revenue)]
    [InlineData(AccountType.Expense)]
    public void Create_WithDifferentAccountTypes_ShouldSetCorrectly(AccountType type)
    {
        var coa = ChartOfAccounts.Create(_tenantId, "100", "Test", type);

        coa.AccountType.Should().Be(type);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var c1 = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var c2 = ChartOfAccounts.Create(_tenantId, "120", "Bankalar", AccountType.Asset);

        c1.Id.Should().NotBe(c2.Id);
    }
}
