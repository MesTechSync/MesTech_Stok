using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class CargoExpenseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var expense = CargoExpense.Create(
            _tenantId, "Yurtici Kargo", 25.50m,
            "ORD-001", "YK-123456");

        expense.Should().NotBeNull();
        expense.CarrierName.Should().Be("Yurtici Kargo");
        expense.Cost.Should().Be(25.50m);
        expense.OrderId.Should().Be("ORD-001");
        expense.TrackingNumber.Should().Be("YK-123456");
    }

    [Fact]
    public void Create_ShouldSetIsBilledToFalse()
    {
        var expense = CargoExpense.Create(_tenantId, "Aras Kargo", 20m);

        expense.IsBilled.Should().BeFalse();
        expense.BilledAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyCarrierName_ShouldThrow()
    {
        var act = () => CargoExpense.Create(_tenantId, "", 20m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullCarrierName_ShouldThrow()
    {
        var act = () => CargoExpense.Create(_tenantId, null!, 20m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeCost_ShouldThrow()
    {
        var act = () => CargoExpense.Create(_tenantId, "Aras", -5m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroCost_ShouldSucceed()
    {
        var expense = CargoExpense.Create(_tenantId, "Surat Kargo", 0m);

        expense.Cost.Should().Be(0m);
    }

    [Fact]
    public void MarkAsBilled_ShouldSetIsBilledAndBilledAt()
    {
        var expense = CargoExpense.Create(_tenantId, "Yurtici Kargo", 25m);

        expense.MarkAsBilled();

        expense.IsBilled.Should().BeTrue();
        expense.BilledAt.Should().NotBeNull();
        expense.BilledAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsBilled_ShouldUpdateUpdatedAt()
    {
        var expense = CargoExpense.Create(_tenantId, "Yurtici Kargo", 25m);

        expense.MarkAsBilled();

        expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullOrderId_ShouldAllowNull()
    {
        var expense = CargoExpense.Create(_tenantId, "Aras", 20m);

        expense.OrderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTrackingNumber_ShouldAllowNull()
    {
        var expense = CargoExpense.Create(_tenantId, "Aras", 20m);

        expense.TrackingNumber.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var e1 = CargoExpense.Create(_tenantId, "Aras", 20m);
        var e2 = CargoExpense.Create(_tenantId, "Yurtici", 25m);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var expense = CargoExpense.Create(_tenantId, "Surat", 15m);

        expense.TenantId.Should().Be(_tenantId);
    }
}
