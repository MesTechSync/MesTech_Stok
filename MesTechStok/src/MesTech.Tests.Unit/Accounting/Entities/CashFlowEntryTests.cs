using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class CashFlowEntryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 1000m,
            CashFlowDirection.Inflow, "Sales", "Platform payment");

        entry.Should().NotBeNull();
        entry.Amount.Should().Be(1000m);
        entry.Direction.Should().Be(CashFlowDirection.Inflow);
        entry.Category.Should().Be("Sales");
        entry.Description.Should().Be("Platform payment");
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        var act = () => CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 0m, CashFlowDirection.Inflow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, -100m, CashFlowDirection.Outflow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Inflow_ShouldSetDirectionCorrectly()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Inflow);

        entry.Direction.Should().Be(CashFlowDirection.Inflow);
    }

    [Fact]
    public void Create_Outflow_ShouldSetDirectionCorrectly()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Outflow);

        entry.Direction.Should().Be(CashFlowDirection.Outflow);
    }

    [Fact]
    public void Create_WithNullCategory_ShouldAllowNull()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Inflow);

        entry.Category.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldAllowNull()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Inflow);

        entry.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithCounterpartyId_ShouldSetCorrectly()
    {
        var counterpartyId = Guid.NewGuid();
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m,
            CashFlowDirection.Inflow, counterpartyId: counterpartyId);

        entry.CounterpartyId.Should().Be(counterpartyId);
    }

    [Fact]
    public void Create_WithNullCounterpartyId_ShouldAllowNull()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Inflow);

        entry.CounterpartyId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetEntryDate()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var entry = CashFlowEntry.Create(
            _tenantId, date, 500m, CashFlowDirection.Inflow);

        entry.EntryDate.Should().Be(date);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var e1 = CashFlowEntry.Create(_tenantId, DateTime.UtcNow, 100m, CashFlowDirection.Inflow);
        var e2 = CashFlowEntry.Create(_tenantId, DateTime.UtcNow, 200m, CashFlowDirection.Outflow);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Create_NavigationProperty_ShouldBeNull()
    {
        var entry = CashFlowEntry.Create(
            _tenantId, DateTime.UtcNow, 500m, CashFlowDirection.Inflow);

        entry.Counterparty.Should().BeNull();
    }
}
