using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformCommission")]
[Trait("Phase", "Dalga5")]
public class PlatformCommissionTests
{
    private static PlatformCommission MakeCommission(
        CommissionType type = CommissionType.Percentage,
        decimal rate = 10m,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool isActive = true,
        DateTime? effectiveTo = null)
        => new()
        {
            TenantId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            Type = type,
            Rate = rate,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            IsActive = isActive,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = effectiveTo,
        };

    [Fact]
    public void Calculate_Percentage_ReturnsCorrectAmount()
    {
        var commission = MakeCommission(CommissionType.Percentage, rate: 15m);
        commission.Calculate(200m).Should().Be(30m); // 200 * 15 / 100
    }

    [Fact]
    public void Calculate_FixedAmount_ReturnsRate()
    {
        var commission = MakeCommission(CommissionType.FixedAmount, rate: 5m);
        commission.Calculate(999m).Should().Be(5m);
    }

    [Fact]
    public void Calculate_Tiered_ThrowsNotSupported()
    {
        var commission = MakeCommission(CommissionType.Tiered, rate: 8m);
        var act = () => commission.Calculate(100m);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*bracket*");
    }

    [Fact]
    public void Calculate_UnknownType_ReturnsZero()
    {
        var commission = MakeCommission((CommissionType)99, rate: 10m);
        commission.Calculate(100m).Should().Be(0m);
    }

    [Fact]
    public void Calculate_Percentage_AppliesMinimumCap()
    {
        // 1% of 100 = 1, but min is 5
        var commission = MakeCommission(CommissionType.Percentage, rate: 1m, minAmount: 5m);
        commission.Calculate(100m).Should().Be(5m);
    }

    [Fact]
    public void Calculate_Percentage_AppliesMaximumCap()
    {
        // 50% of 200 = 100, but max is 30
        var commission = MakeCommission(CommissionType.Percentage, rate: 50m, maxAmount: 30m);
        commission.Calculate(200m).Should().Be(30m);
    }

    [Fact]
    public void Calculate_RoundsToTwoDecimalPlaces()
    {
        var commission = MakeCommission(CommissionType.Percentage, rate: 10m);
        commission.Calculate(33.33m).Should().Be(3.33m); // 33.33 * 10 / 100 = 3.333 → 3.33
    }

    [Fact]
    public void IsEffective_ActiveWithinDateRange_ReturnsTrue()
    {
        var commission = MakeCommission(isActive: true, effectiveTo: DateTime.UtcNow.AddDays(30));
        commission.IsEffective(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_Expired_ReturnsFalse()
    {
        var commission = MakeCommission(isActive: true, effectiveTo: DateTime.UtcNow.AddDays(-1));
        commission.IsEffective(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_Inactive_ReturnsFalse()
    {
        var commission = MakeCommission(isActive: false);
        commission.IsEffective(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_NoEndDate_AlwaysActive()
    {
        var commission = MakeCommission(isActive: true, effectiveTo: null);
        commission.IsEffective(DateTime.UtcNow.AddYears(10)).Should().BeTrue();
    }

    [Fact]
    public void ToString_ContainsPlatformAndRate()
    {
        var commission = MakeCommission(CommissionType.Percentage, rate: 12m, isActive: true);
        var str = commission.ToString();
        str.Should().Contain("Trendyol");
        str.Should().Contain("12");
        str.Should().Contain("Aktif");
    }
}
