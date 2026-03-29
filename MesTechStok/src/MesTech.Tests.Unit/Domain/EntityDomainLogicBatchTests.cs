using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 6: Entity domain logic tests
// ALAN_GENISLEME: Seçenek A — Entity test kapsam artırma
// WarehouseBin state machine + StockAlertRule business rules
// ════════════════════════════════════════════════════════

#region WarehouseBin

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WarehouseBinDomainTests
{
    private static WarehouseBin CreateBin() => new()
    {
        TenantId = Guid.NewGuid(),
        Name = "B-01",
        Code = "WH1-R1-S1-B01",
        ShelfId = Guid.NewGuid(),
        BinNumber = 1
    };

    // ── Defaults ──

    [Fact]
    public void NewBin_IsActiveByDefault()
    {
        var bin = CreateBin();
        bin.IsActive.Should().BeTrue();
        bin.IsReserved.Should().BeFalse();
        bin.IsLocked.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    // ── Activate / Deactivate ──

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var bin = CreateBin();
        bin.Deactivate();
        bin.IsActive.Should().BeFalse();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var bin = CreateBin();
        bin.Deactivate();
        bin.Activate();
        bin.IsActive.Should().BeTrue();
    }

    // ── Reserve ──

    [Fact]
    public void Reserve_SetsIsReserved()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.IsReserved.Should().BeTrue();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Reserve_WhenLocked_ThrowsInvalidOperation()
    {
        var bin = CreateBin();
        bin.Lock();

        var act = () => bin.Reserve();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*kilitli*");
    }

    [Fact]
    public void ReleaseReservation_ClearsReserved()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.ReleaseReservation();
        bin.IsReserved.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    // ── Lock ──

    [Fact]
    public void Lock_SetsLockedAndClearsReservation()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.IsReserved.Should().BeTrue();

        bin.Lock();
        bin.IsLocked.Should().BeTrue();
        bin.IsReserved.Should().BeFalse(); // Kilit rezervasyonu iptal eder
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Unlock_ClearsLocked()
    {
        var bin = CreateBin();
        bin.Lock();
        bin.Unlock();
        bin.IsLocked.Should().BeFalse();
        bin.IsAvailable.Should().BeTrue();
    }

    // ── IsAvailable composite ──

    [Fact]
    public void IsAvailable_FalseWhenInactive()
    {
        var bin = CreateBin();
        bin.Deactivate();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_FalseWhenReserved()
    {
        var bin = CreateBin();
        bin.Reserve();
        bin.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_FalseWhenLocked()
    {
        var bin = CreateBin();
        bin.Lock();
        bin.IsAvailable.Should().BeFalse();
    }
}

#endregion

#region StockAlertRule

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class StockAlertRuleDomainTests
{
    // ── Factory: Create ──

    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var rule = StockAlertRule.Create(tenantId, productId, warningThreshold: 10, criticalThreshold: 3);

        rule.TenantId.Should().Be(tenantId);
        rule.ProductId.Should().Be(productId);
        rule.WarningThreshold.Should().Be(10);
        rule.CriticalThreshold.Should().Be(3);
        rule.IsActive.Should().BeTrue();
        rule.AutoReorderEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_NegativeWarning_Throws()
    {
        var act = () => StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), -1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_CriticalGreaterThanWarning_Throws()
    {
        var act = () => StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 5, 10);
        act.Should().Throw<ArgumentException>().WithMessage("*CriticalThreshold*");
    }

    [Fact]
    public void Create_CriticalEqualsWarning_Throws()
    {
        var act = () => StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 5, 5);
        act.Should().Throw<ArgumentException>();
    }

    // ── EvaluateStock ──

    [Theory]
    [InlineData(100, 10, 3, StockAlertLevel.Normal)]
    [InlineData(10, 10, 3, StockAlertLevel.Warning)]
    [InlineData(7, 10, 3, StockAlertLevel.Warning)]
    [InlineData(3, 10, 3, StockAlertLevel.Critical)]
    [InlineData(0, 10, 3, StockAlertLevel.Critical)]
    public void EvaluateStock_ReturnsCorrectLevel(int stock, int warning, int critical, StockAlertLevel expected)
    {
        var rule = StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), warning, critical);
        rule.EvaluateStock(stock).Should().Be(expected);
    }

    // ── ShouldAutoReorder ──

    [Fact]
    public void ShouldAutoReorder_WhenEnabledAndCritical_ReturnsTrue()
    {
        var rule = StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 3,
            autoReorder: true, reorderQty: 50);

        rule.ShouldAutoReorder(currentStock: 2).Should().BeTrue();
    }

    [Fact]
    public void ShouldAutoReorder_WhenDisabled_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 3,
            autoReorder: false, reorderQty: 50);

        rule.ShouldAutoReorder(currentStock: 2).Should().BeFalse();
    }

    [Fact]
    public void ShouldAutoReorder_WhenStockAboveCritical_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 3,
            autoReorder: true, reorderQty: 50);

        rule.ShouldAutoReorder(currentStock: 5).Should().BeFalse();
    }

    [Fact]
    public void ShouldAutoReorder_WhenNoReorderQty_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 3,
            autoReorder: true, reorderQty: null);

        rule.ShouldAutoReorder(currentStock: 2).Should().BeFalse();
    }
}

#endregion
