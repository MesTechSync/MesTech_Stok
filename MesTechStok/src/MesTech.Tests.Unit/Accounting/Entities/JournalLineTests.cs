using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class JournalLineTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var entryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var line = JournalLine.Create(entryId, accountId, 100m, 0m, "Debit line");

        line.Should().NotBeNull();
        line.JournalEntryId.Should().Be(entryId);
        line.AccountId.Should().Be(accountId);
        line.Debit.Should().Be(100m);
        line.Credit.Should().Be(0m);
        line.Description.Should().Be("Debit line");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);

        line.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);

        line.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCreditOnly_ShouldSucceed()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 500m);

        line.Debit.Should().Be(0m);
        line.Credit.Should().Be(500m);
    }

    [Fact]
    public void Create_WithBothDebitAndCredit_ShouldSucceed()
    {
        // The entity does not enforce mutual exclusivity at the line level;
        // the JournalEntry.AddLine method enforces the zero-zero rule.
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 50m);

        line.Debit.Should().Be(100m);
        line.Credit.Should().Be(50m);
    }

    [Fact]
    public void Create_WithNullDescription_ShouldAllowNull()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);

        line.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithDescription_ShouldSetDescription()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 200m, "Credit entry");

        line.Description.Should().Be("Credit entry");
    }

    [Fact]
    public void Create_ShouldSetJournalEntryId()
    {
        var entryId = Guid.NewGuid();
        var line = JournalLine.Create(entryId, Guid.NewGuid(), 100m, 0m);

        line.JournalEntryId.Should().Be(entryId);
    }

    [Fact]
    public void Create_ShouldSetAccountId()
    {
        var accountId = Guid.NewGuid();
        var line = JournalLine.Create(Guid.NewGuid(), accountId, 100m, 0m);

        line.AccountId.Should().Be(accountId);
    }

    [Fact]
    public void Create_MultipleLines_ShouldHaveUniqueIds()
    {
        var line1 = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);
        var line2 = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 100m);

        line1.Id.Should().NotBe(line2.Id);
    }

    [Fact]
    public void Create_WithLargeAmounts_ShouldHandleCorrectly()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 999_999_999.99m, 0m);

        line.Debit.Should().Be(999_999_999.99m);
    }

    [Fact]
    public void Create_WithDecimalPrecision_ShouldPreserve()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 123.456789m, 0m);

        line.Debit.Should().Be(123.456789m);
    }

    [Fact]
    public void Create_ShouldHaveNullNavigationProperties()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);

        line.JournalEntry.Should().BeNull();
        line.Account.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroDebit_ShouldSucceed()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, 100m);

        line.Debit.Should().Be(0m);
    }

    [Fact]
    public void Create_WithZeroCredit_ShouldSucceed()
    {
        var line = JournalLine.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m);

        line.Credit.Should().Be(0m);
    }
}
