using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class StockAlertRuleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void Create_ValidThresholds_ReturnsRule()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, warningThreshold: 10, criticalThreshold: 3);

        rule.TenantId.Should().Be(_tenantId);
        rule.ProductId.Should().Be(_productId);
        rule.WarningThreshold.Should().Be(10);
        rule.CriticalThreshold.Should().Be(3);
        rule.IsActive.Should().BeTrue();
        rule.AutoReorderEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_CriticalGteWarning_Throws()
    {
        var act = () => StockAlertRule.Create(_tenantId, _productId, warningThreshold: 5, criticalThreshold: 5);
        act.Should().Throw<ArgumentException>().WithParameterName("criticalThreshold");
    }

    [Fact]
    public void Create_CriticalGreaterThanWarning_Throws()
    {
        var act = () => StockAlertRule.Create(_tenantId, _productId, warningThreshold: 3, criticalThreshold: 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NegativeWarning_Throws()
    {
        var act = () => StockAlertRule.Create(_tenantId, _productId, warningThreshold: -1, criticalThreshold: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EvaluateStock_AboveWarning_ReturnsNormal()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3);
        rule.EvaluateStock(15).Should().Be(StockAlertLevel.Normal);
    }

    [Fact]
    public void EvaluateStock_AtWarning_ReturnsWarning()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3);
        rule.EvaluateStock(10).Should().Be(StockAlertLevel.Warning);
    }

    [Fact]
    public void EvaluateStock_BetweenWarningAndCritical_ReturnsWarning()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3);
        rule.EvaluateStock(5).Should().Be(StockAlertLevel.Warning);
    }

    [Fact]
    public void EvaluateStock_AtCritical_ReturnsCritical()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3);
        rule.EvaluateStock(3).Should().Be(StockAlertLevel.Critical);
    }

    [Fact]
    public void EvaluateStock_BelowCritical_ReturnsCritical()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3);
        rule.EvaluateStock(0).Should().Be(StockAlertLevel.Critical);
    }

    [Fact]
    public void ShouldAutoReorder_EnabledAndBelowCritical_ReturnsTrue()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3,
            autoReorder: true, reorderQty: 50);
        rule.ShouldAutoReorder(2).Should().BeTrue();
    }

    [Fact]
    public void ShouldAutoReorder_DisabledEvenBelowCritical_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3,
            autoReorder: false, reorderQty: 50);
        rule.ShouldAutoReorder(0).Should().BeFalse();
    }

    [Fact]
    public void ShouldAutoReorder_AboveCritical_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3,
            autoReorder: true, reorderQty: 50);
        rule.ShouldAutoReorder(5).Should().BeFalse();
    }

    [Fact]
    public void ShouldAutoReorder_ZeroReorderQty_ReturnsFalse()
    {
        var rule = StockAlertRule.Create(_tenantId, _productId, 10, 3,
            autoReorder: true, reorderQty: 0);
        rule.ShouldAutoReorder(1).Should().BeFalse();
    }
}
