using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class PersonalExpenseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Ofis Malzemesi", 250m,
            DateTime.UtcNow, ExpenseSource.Manual, "Kirtasiye");

        expense.Should().NotBeNull();
        expense.Title.Should().Be("Ofis Malzemesi");
        expense.Amount.Should().Be(250m);
        expense.Category.Should().Be("Kirtasiye");
        expense.Source.Should().Be(ExpenseSource.Manual);
    }

    [Fact]
    public void Create_ShouldSetIsApprovedToFalse()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        expense.IsApproved.Should().BeFalse();
        expense.ApprovedBy.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRaiseExpenseCreatedEvent()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Kahve", 50m,
            DateTime.UtcNow, ExpenseSource.WhatsApp);

        expense.DomainEvents.Should().ContainSingle(e => e is ExpenseCreatedEvent);
        var evt = expense.DomainEvents.OfType<ExpenseCreatedEvent>().Single();
        evt.Title.Should().Be("Kahve");
        evt.Amount.Should().Be(50m);
        evt.Source.Should().Be(ExpenseSource.WhatsApp);
    }

    [Fact]
    public void Approve_ShouldSetIsApprovedAndApprovedBy()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        expense.Approve("admin@mestech.com");

        expense.IsApproved.Should().BeTrue();
        expense.ApprovedBy.Should().Be("admin@mestech.com");
    }

    [Fact]
    public void Approve_WithEmptyApprover_ShouldThrow()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        var act = () => expense.Approve("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => PersonalExpense.Create(
            _tenantId, "", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        var act = () => PersonalExpense.Create(
            _tenantId, "Test", 0m,
            DateTime.UtcNow, ExpenseSource.Manual);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => PersonalExpense.Create(
            _tenantId, "Test", -50m,
            DateTime.UtcNow, ExpenseSource.Manual);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(ExpenseSource.Manual)]
    [InlineData(ExpenseSource.WhatsApp)]
    [InlineData(ExpenseSource.Telegram)]
    [InlineData(ExpenseSource.Email)]
    [InlineData(ExpenseSource.AI)]
    public void Create_WithDifferentSources_ShouldSetCorrectly(ExpenseSource source)
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, source);

        expense.Source.Should().Be(source);
    }

    [Fact]
    public void Create_WithNullCategory_ShouldAllowNull()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        expense.Category.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetExpenseDate()
    {
        var date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m, date, ExpenseSource.Manual);

        expense.ExpenseDate.Should().Be(date);
    }

    [Fact]
    public void Approve_ShouldUpdateUpdatedAt()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        expense.Approve("admin");

        expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var e1 = PersonalExpense.Create(_tenantId, "Test 1", 100m, DateTime.UtcNow, ExpenseSource.Manual);
        var e2 = PersonalExpense.Create(_tenantId, "Test 2", 200m, DateTime.UtcNow, ExpenseSource.Manual);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var expense = PersonalExpense.Create(
            _tenantId, "Test", 100m,
            DateTime.UtcNow, ExpenseSource.Manual);

        expense.TenantId.Should().Be(_tenantId);
    }
}
