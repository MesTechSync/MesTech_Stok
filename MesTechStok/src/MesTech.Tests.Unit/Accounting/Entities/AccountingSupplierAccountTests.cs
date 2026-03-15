using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class AccountingSupplierAccountTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var supplierId = Guid.NewGuid();
        var account = AccountingSupplierAccount.Create(
            _tenantId, supplierId, "Tedarikci A");

        account.Should().NotBeNull();
        account.SupplierId.Should().Be(supplierId);
        account.Name.Should().Be("Tedarikci A");
        account.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_ShouldSetBalanceToZero()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Create_WithCustomCurrency_ShouldSetCorrectly()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "USD Supplier", "USD");

        account.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdjustBalance_ShouldIncreaseBalance()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.AdjustBalance(1000m);

        account.Balance.Should().Be(1000m);
    }

    [Fact]
    public void AdjustBalance_ShouldDecreaseBalance()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");
        account.AdjustBalance(1000m);

        account.AdjustBalance(-300m);

        account.Balance.Should().Be(700m);
    }

    [Fact]
    public void AdjustBalance_ShouldUpdateLastTransactionDate()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.AdjustBalance(500m);

        account.LastTransactionDate.Should().NotBeNull();
        account.LastTransactionDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdjustBalance_ShouldUpdateUpdatedAt()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.AdjustBalance(500m);

        account.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdjustBalance_MultipleTimes_ShouldAccumulate()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.AdjustBalance(1000m);
        account.AdjustBalance(500m);
        account.AdjustBalance(-200m);

        account.Balance.Should().Be(1300m);
    }

    [Fact]
    public void Create_LastTransactionDate_ShouldBeNull()
    {
        var account = AccountingSupplierAccount.Create(
            _tenantId, Guid.NewGuid(), "Test");

        account.LastTransactionDate.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var a1 = AccountingSupplierAccount.Create(_tenantId, Guid.NewGuid(), "A");
        var a2 = AccountingSupplierAccount.Create(_tenantId, Guid.NewGuid(), "B");

        a1.Id.Should().NotBe(a2.Id);
    }
}
