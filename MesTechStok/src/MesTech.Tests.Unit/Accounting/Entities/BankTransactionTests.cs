using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class BankTransactionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1500.50m, "HAVALE - TRENDYOL", "REF-001");

        tx.Should().NotBeNull();
        tx.TenantId.Should().Be(_tenantId);
        tx.BankAccountId.Should().Be(_bankAccountId);
        tx.Amount.Should().Be(1500.50m);
        tx.Description.Should().Be("HAVALE - TRENDYOL");
        tx.ReferenceNumber.Should().Be("REF-001");
    }

    [Fact]
    public void Create_ShouldSetIsReconciledToFalse()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public void Create_IdempotencyKey_ShouldBePopulated()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.IdempotencyKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_WithCustomIdempotencyKey_ShouldUseProvided()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test", idempotencyKey: "custom-key-123");

        tx.IdempotencyKey.Should().Be("custom-key-123");
    }

    [Fact]
    public void Create_WithNullIdempotencyKey_ShouldAutoGenerate()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.IdempotencyKey.Should().NotBeNull();
        tx.IdempotencyKey!.Length.Should().Be(32); // Guid.ToString("N")
    }

    [Fact]
    public void Reconcile_ShouldSetIsReconciled()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.MarkReconciled();

        tx.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_ShouldUpdateUpdatedAt()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.MarkReconciled();

        tx.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyDescription_ShouldThrow()
    {
        var act = () => BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldThrow()
    {
        var act = () => BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldSucceed()
    {
        // Debit transactions have negative amounts
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            -500m, "ODEME");

        tx.Amount.Should().Be(-500m);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        var act = () => BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            0m, "Zero transaction");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldSetTransactionDate()
    {
        var date = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, date,
            1000m, "Test");

        tx.TransactionDate.Should().Be(date);
    }

    [Fact]
    public void Create_WithNullReferenceNumber_ShouldAllowNull()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.ReferenceNumber.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var tx1 = BankTransaction.Create(_tenantId, _bankAccountId, DateTime.UtcNow, 100m, "Test 1");
        var tx2 = BankTransaction.Create(_tenantId, _bankAccountId, DateTime.UtcNow, 200m, "Test 2");

        tx1.Id.Should().NotBe(tx2.Id);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var tx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow,
            1000m, "Test");

        tx.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_MultipleTransactions_ShouldHaveDifferentIdempotencyKeys()
    {
        var tx1 = BankTransaction.Create(_tenantId, _bankAccountId, DateTime.UtcNow, 100m, "Test 1");
        var tx2 = BankTransaction.Create(_tenantId, _bankAccountId, DateTime.UtcNow, 100m, "Test 1");

        tx1.IdempotencyKey.Should().NotBe(tx2.IdempotencyKey);
    }
}
