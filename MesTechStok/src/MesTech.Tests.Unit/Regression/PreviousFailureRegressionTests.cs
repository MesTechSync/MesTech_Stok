using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Tests.Unit.Regression;

/// <summary>
/// Regression tests for previously found/fixed issues.
/// Ensures earlier fixes remain intact across refactorings.
/// Dalga 14+15 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class PreviousFailureRegressionTests
{
    #region StockChangedEvent — TenantId required

    [Fact]
    public void StockChangedEvent_RequiresTenantId_InConstructor()
    {
        // Regression: StockChangedEvent must carry TenantId for multi-tenant event routing
        var tenantId = Guid.NewGuid();
        var evt = new StockChangedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: tenantId,
            SKU: "TEST-SKU",
            PreviousQuantity: 10,
            NewQuantity: 5,
            MovementType: StockMovementType.StockOut,
            OccurredAt: DateTime.UtcNow);

        evt.TenantId.Should().Be(tenantId);
        evt.TenantId.Should().NotBeEmpty();
    }

    [Fact]
    public void StockChangedEvent_EmptyTenantId_ShouldBeDetectable()
    {
        // Ensure we can detect empty TenantId (for validation layers)
        var evt = new StockChangedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: Guid.Empty,
            SKU: "TEST-SKU",
            PreviousQuantity: 0,
            NewQuantity: 10,
            MovementType: StockMovementType.StockIn,
            OccurredAt: DateTime.UtcNow);

        evt.TenantId.Should().Be(Guid.Empty);
    }

    #endregion

    #region PriceChangedEvent — TenantId required

    [Fact]
    public void PriceChangedEvent_RequiresTenantId_InConstructor()
    {
        // Regression: PriceChangedEvent must carry TenantId for multi-tenant routing
        var tenantId = Guid.NewGuid();
        var evt = new PriceChangedEvent(
            ProductId: Guid.NewGuid(),
            TenantId: tenantId,
            SKU: "PRICE-SKU",
            OldPrice: 100m,
            NewPrice: 150m,
            OccurredAt: DateTime.UtcNow);

        evt.TenantId.Should().Be(tenantId);
        evt.TenantId.Should().NotBeEmpty();
    }

    #endregion

    #region CargoProvider — member count guard

    [Fact]
    public void CargoProvider_ShouldHave12Members_IncludingDHLAndFedEx()
    {
        // Regression: CargoProvider enum must have exactly 12 members
        // None + 10 providers (YurticiKargo..FedEx) + Other
        var values = Enum.GetValues<CargoProvider>();

        values.Should().HaveCount(12);
        values.Should().Contain(CargoProvider.DHL);
        values.Should().Contain(CargoProvider.FedEx);
        values.Should().Contain(CargoProvider.None);
        values.Should().Contain(CargoProvider.YurticiKargo);
        values.Should().Contain(CargoProvider.Other);
    }

    [Fact]
    public void CargoProvider_DHL_ShouldBe9_FedEx_ShouldBe10()
    {
        // Explicit ordinal values — ensure no accidental reordering
        ((int)CargoProvider.DHL).Should().Be(9);
        ((int)CargoProvider.FedEx).Should().Be(10);
    }

    #endregion

    #region BudgetPlan.Variance — computed property

    [Fact]
    public void BudgetPlan_Variance_IsComputedCorrectly()
    {
        // Variance = (ActualRevenue - ActualExpense) - (PlannedRevenue - PlannedExpense)
        var plan = new BudgetPlan
        {
            TenantId = Guid.NewGuid(),
            Name = "Q1 2026",
            Period = "2026-Q1",
            PlannedRevenue = 100_000m,
            PlannedExpense = 60_000m,
            ActualRevenue = 120_000m,
            ActualExpense = 70_000m
        };

        // Actual profit = 120k - 70k = 50k
        // Planned profit = 100k - 60k = 40k
        // Variance = 50k - 40k = 10k (positive = over-performing)
        plan.Variance.Should().Be(10_000m);
    }

    [Fact]
    public void BudgetPlan_Variance_NegativeWhenUnderPerforming()
    {
        var plan = new BudgetPlan
        {
            TenantId = Guid.NewGuid(),
            Name = "Q2 2026",
            Period = "2026-Q2",
            PlannedRevenue = 100_000m,
            PlannedExpense = 60_000m,
            ActualRevenue = 80_000m,
            ActualExpense = 65_000m
        };

        // Actual profit = 80k - 65k = 15k
        // Planned profit = 100k - 60k = 40k
        // Variance = 15k - 40k = -25k (under-performing)
        plan.Variance.Should().Be(-25_000m);
    }

    [Fact]
    public void BudgetPlan_Variance_ZeroWhenExactlyOnTarget()
    {
        var plan = new BudgetPlan
        {
            TenantId = Guid.NewGuid(),
            Name = "Q3 2026",
            Period = "2026-Q3",
            PlannedRevenue = 100_000m,
            PlannedExpense = 60_000m,
            ActualRevenue = 100_000m,
            ActualExpense = 60_000m
        };

        plan.Variance.Should().Be(0m);
    }

    #endregion

    #region FixedAsset.NetBookValue — computed property

    [Fact]
    public void FixedAsset_NetBookValue_IsComputedCorrectly()
    {
        // NetBookValue = AcquisitionCost - AccumulatedDepreciation
        var asset = FixedAsset.Create(
            tenantId: Guid.NewGuid(),
            name: "CNC Tezgahi",
            assetCode: "253",
            acquisitionCost: 100_000m,
            acquisitionDate: new DateTime(2025, 1, 1),
            usefulLifeYears: 5,
            method: DepreciationMethod.StraightLine);

        // Initially: NBV = 100k - 0 = 100k
        asset.NetBookValue.Should().Be(100_000m);

        // After 20k depreciation: NBV = 100k - 20k = 80k
        asset.ApplyDepreciation(20_000m);
        asset.NetBookValue.Should().Be(80_000m);
    }

    [Fact]
    public void FixedAsset_NetBookValue_ZeroAfterFullDepreciation()
    {
        var asset = FixedAsset.Create(
            tenantId: Guid.NewGuid(),
            name: "Demirbaslar",
            assetCode: "255",
            acquisitionCost: 10_000m,
            acquisitionDate: new DateTime(2024, 1, 1),
            usefulLifeYears: 2,
            method: DepreciationMethod.StraightLine);

        asset.ApplyDepreciation(5_000m);
        asset.ApplyDepreciation(5_000m);

        asset.NetBookValue.Should().Be(0m);
    }

    [Fact]
    public void FixedAsset_ApplyDepreciation_CannotExceedCost()
    {
        var asset = FixedAsset.Create(
            tenantId: Guid.NewGuid(),
            name: "Ford Transit",
            assetCode: "254",
            acquisitionCost: 50_000m,
            acquisitionDate: new DateTime(2025, 6, 1),
            usefulLifeYears: 5,
            method: DepreciationMethod.DecliningBalance);

        // Try to depreciate more than acquisition cost
        var act = () => asset.ApplyDepreciation(50_001m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*maliyeti asamaz*");
    }

    #endregion
}
