using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class FinancialGoalTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Aylik Hedef",
            100_000m, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.Should().NotBeNull();
        goal.Title.Should().Be("Aylik Hedef");
        goal.TargetAmount.Should().Be(100_000m);
    }

    [Fact]
    public void Create_ShouldSetDefaultCurrentAmountToZero()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test",
            50_000m, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.CurrentAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_ShouldSetIsAchievedToFalse()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test",
            50_000m, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.IsAchieved.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => FinancialGoal.Create(
            _tenantId, "", 50_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroTargetAmount_ShouldThrow()
    {
        var act = () => FinancialGoal.Create(
            _tenantId, "Test", 0m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeTargetAmount_ShouldThrow()
    {
        var act = () => FinancialGoal.Create(
            _tenantId, "Test", -1000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldThrow()
    {
        var act = () => FinancialGoal.Create(
            _tenantId, "Test", 50_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_ShouldThrow()
    {
        var now = DateTime.UtcNow;
        var act = () => FinancialGoal.Create(
            _tenantId, "Test", 50_000m, now, now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProgress_ShouldSetCurrentAmount()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test", 100_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.UpdateProgress(50_000m);

        goal.CurrentAmount.Should().Be(50_000m);
    }

    [Fact]
    public void UpdateProgress_WhenReachTarget_ShouldSetIsAchieved()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test", 100_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.UpdateProgress(100_000m);

        goal.IsAchieved.Should().BeTrue();
    }

    [Fact]
    public void UpdateProgress_WhenExceedTarget_ShouldSetIsAchieved()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test", 100_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.UpdateProgress(150_000m);

        goal.IsAchieved.Should().BeTrue();
    }

    [Fact]
    public void UpdateProgress_WhenBelowTarget_ShouldNotSetIsAchieved()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test", 100_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.UpdateProgress(50_000m);

        goal.IsAchieved.Should().BeFalse();
    }

    [Fact]
    public void UpdateProgress_ShouldUpdateUpdatedAt()
    {
        var goal = FinancialGoal.Create(
            _tenantId, "Test", 100_000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        goal.UpdateProgress(50_000m);

        goal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldSetDates()
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var goal = FinancialGoal.Create(_tenantId, "Test", 50_000m, start, end);

        goal.StartDate.Should().Be(start);
        goal.EndDate.Should().Be(end);
    }
}
