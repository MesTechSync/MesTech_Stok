using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class FixedExpenseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expense = FixedExpense.Create(
            _tenantId, "Ofis Kirasi", 15000m,
            dayOfMonth: 1, startDate: startDate);

        expense.Should().NotBeNull();
        expense.Name.Should().Be("Ofis Kirasi");
        expense.MonthlyAmount.Should().Be(15000m);
        expense.DayOfMonth.Should().Be(1);
        expense.StartDate.Should().Be(startDate);
        expense.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => FixedExpense.Create(
            _tenantId, "", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => FixedExpense.Create(
            _tenantId, null!, 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        var act = () => FixedExpense.Create(
            _tenantId, "Test", 0m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => FixedExpense.Create(
            _tenantId, "Test", -100m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Create_WithInvalidDayOfMonth_ShouldThrow(int day)
    {
        var act = () => FixedExpense.Create(
            _tenantId, "Test", 500m,
            dayOfMonth: day, startDate: DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.Deactivate();

        expense.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetEndDate()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.Deactivate();

        expense.EndDate.Should().NotBeNull();
        expense.EndDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_ShouldUpdateUpdatedAt()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.Deactivate();

        expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndClearEndDate()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.Deactivate();
        expense.Activate();

        expense.IsActive.Should().BeTrue();
        expense.EndDate.Should().BeNull();
    }

    [Fact]
    public void UpdateAmount_WithValidAmount_ShouldUpdate()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.UpdateAmount(750m);

        expense.MonthlyAmount.Should().Be(750m);
    }

    [Fact]
    public void UpdateAmount_WithZeroAmount_ShouldThrow()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        var act = () => expense.UpdateAmount(0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateAmount_WithNegativeAmount_ShouldThrow()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        var act = () => expense.UpdateAmount(-100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateAmount_ShouldUpdateUpdatedAt()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.UpdateAmount(750m);

        expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCustomCurrency_ShouldSet()
    {
        var expense = FixedExpense.Create(
            _tenantId, "AWS Server", 200m,
            dayOfMonth: 1, startDate: DateTime.UtcNow,
            currency: "USD");

        expense.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithEndDate_ShouldSet()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var expense = FixedExpense.Create(
            _tenantId, "Lease", 10000m,
            dayOfMonth: 1, startDate: start, endDate: end);

        expense.EndDate.Should().Be(end);
    }

    [Fact]
    public void Create_WithSupplierInfo_ShouldSet()
    {
        var supplierId = Guid.NewGuid();
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow,
            supplierName: "Turk Telekom", supplierId: supplierId);

        expense.SupplierName.Should().Be("Turk Telekom");
        expense.SupplierId.Should().Be(supplierId);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var e1 = FixedExpense.Create(_tenantId, "Kira", 10000m,
            dayOfMonth: 1, startDate: DateTime.UtcNow);
        var e2 = FixedExpense.Create(_tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var expense = FixedExpense.Create(
            _tenantId, "Internet", 500m,
            dayOfMonth: 15, startDate: DateTime.UtcNow);

        expense.TenantId.Should().Be(_tenantId);
    }
}
