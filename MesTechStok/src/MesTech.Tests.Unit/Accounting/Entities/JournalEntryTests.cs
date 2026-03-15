using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class JournalEntryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test entry");

        entry.Should().NotBeNull();
        entry.TenantId.Should().Be(_tenantId);
        entry.Description.Should().Be("Test entry");
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        entry.Id.Should().NotBeEmpty();
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entry.IsDeleted.Should().BeFalse();
        entry.IsPosted.Should().BeFalse();
        entry.PostedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithReferenceNumber_ShouldSetReferenceNumber()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test", "REF-001");

        entry.ReferenceNumber.Should().Be("REF-001");
    }

    [Fact]
    public void Create_WithNullReferenceNumber_ShouldAllowNull()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        entry.ReferenceNumber.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyDescription_ShouldThrow()
    {
        var act = () => JournalEntry.Create(_tenantId, DateTime.UtcNow, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceDescription_ShouldThrow()
    {
        var act = () => JournalEntry.Create(_tenantId, DateTime.UtcNow, "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldThrow()
    {
        var act = () => JournalEntry.Create(_tenantId, DateTime.UtcNow, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddLine_ShouldAddToLines()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        var accountId = Guid.NewGuid();

        entry.AddLine(accountId, 100m, 0m, "Debit line");

        entry.Lines.Should().HaveCount(1);
        entry.Lines[0].Debit.Should().Be(100m);
    }

    [Fact]
    public void AddLine_WhenPosted_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        entry.AddLine(accountId1, 100m, 0m);
        entry.AddLine(accountId2, 0m, 100m);
        entry.Post();

        var act = () => entry.AddLine(Guid.NewGuid(), 50m, 0m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*posted*");
    }

    [Fact]
    public void AddLine_WithNegativeDebit_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        var act = () => entry.AddLine(Guid.NewGuid(), -100m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddLine_WithNegativeCredit_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        var act = () => entry.AddLine(Guid.NewGuid(), 0m, -100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddLine_WithZeroDebitAndCredit_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        var act = () => entry.AddLine(Guid.NewGuid(), 0m, 0m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*debit or credit must be greater than zero*");
    }

    [Fact]
    public void Validate_WithBalancedLines_ShouldNotThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        var act = () => entry.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithUnbalancedLines_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 50m);

        var act = () => entry.Validate();

        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void Validate_WithLessThanTwoLines_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);

        var act = () => entry.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 2 lines*");
    }

    [Fact]
    public void Post_ShouldSetIsPostedAndPostedAt()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        entry.Post();

        entry.IsPosted.Should().BeTrue();
        entry.PostedAt.Should().NotBeNull();
        entry.PostedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Post_AlreadyPosted_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);
        entry.Post();

        var act = () => entry.Post();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already posted*");
    }

    [Fact]
    public void Post_ShouldRaiseLedgerPostedEvent()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 250m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 250m);

        entry.Post();

        entry.DomainEvents.Should().ContainSingle(e => e is LedgerPostedEvent);
        var evt = entry.DomainEvents.OfType<LedgerPostedEvent>().Single();
        evt.TenantId.Should().Be(_tenantId);
        evt.JournalEntryId.Should().Be(entry.Id);
        evt.TotalAmount.Should().Be(250m);
        evt.LineCount.Should().Be(2);
    }

    [Fact]
    public void Lines_ShouldBeReadOnly()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);

        entry.Lines.Should().BeAssignableTo<IReadOnlyList<JournalLine>>();
    }

    [Fact]
    public void Create_ShouldSetEntryDate()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var entry = JournalEntry.Create(_tenantId, date, "Test");

        entry.EntryDate.Should().Be(date);
    }

    [Fact]
    public void Post_WithUnbalancedLines_ShouldThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 80m);

        var act = () => entry.Post();

        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void AddLine_MultipleTimes_ShouldAccumulateLines()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 200m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 300m);

        entry.Lines.Should().HaveCount(3);
    }

    [Fact]
    public void Post_ShouldUpdateUpdatedAt()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        entry.Post();

        entry.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Validate_WithMultipleBalancedLines_ShouldNotThrow()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 50m, 0m);
        entry.AddLine(Guid.NewGuid(), 50m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        var act = () => entry.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void JournalEntryImbalanceException_ShouldContainAmounts()
    {
        var ex = new JournalEntryImbalanceException(100m, 80m);

        ex.TotalDebit.Should().Be(100m);
        ex.TotalCredit.Should().Be(80m);
        ex.Message.Should().Contain("100");
        ex.Message.Should().Contain("80");
    }

    [Fact]
    public void Create_ShouldImplementITenantEntity()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test");

        entry.TenantId.Should().Be(_tenantId);
    }
}
