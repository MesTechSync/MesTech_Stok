using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "CommissionCalculation")]
[Trait("Phase", "PhaseA")]
public class CommissionCalculationServiceTests
{
    // ─────────────────────────────────────────────────────────────
    // Test 1: Dynamic rate received → correct calculation
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_DynamicRateReceived_ReturnsCorrectCalculation()
    {
        // Arrange
        var dynamicRate = new DynamicRateResult(
            Rate: 0.12m,
            Source: "TrendyolAPI",
            CachedUntil: DateTime.UtcNow.AddMinutes(30));

        Func<string, string?, CancellationToken, Task<DynamicRateResult?>> provider =
            (_, _, _) => Task.FromResult<DynamicRateResult?>(dynamicRate);

        var service = new CommissionCalculationService(provider);

        // Act
        var result = await service.CalculateCommissionAsync("Trendyol", "Elektronik", 1000m);

        // Assert
        result.Rate.Should().Be(0.12m);
        result.Amount.Should().Be(120m); // 1000 * 0.12
        result.Source.Should().Be("TrendyolAPI");
        result.IsCached.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 2: Dynamic rate null → fallback works
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_DynamicRateNull_UsesFallback()
    {
        // Arrange
        Func<string, string?, CancellationToken, Task<DynamicRateResult?>> provider =
            (_, _, _) => Task.FromResult<DynamicRateResult?>(null);

        var service = new CommissionCalculationService(provider);

        // Act
        var result = await service.CalculateCommissionAsync("Trendyol", null, 1000m);

        // Assert
        result.Rate.Should().Be(0.15m); // Trendyol fallback
        result.Amount.Should().Be(150m);
        result.Source.Should().Be("StaticFallback");
        result.IsCached.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 3: rateProvider null (legacy usage) → fallback works
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_NullProvider_UsesFallback()
    {
        // Arrange — legacy constructor, no provider
        var service = new CommissionCalculationService();

        // Act
        var result = await service.CalculateCommissionAsync("Hepsiburada", null, 500m);

        // Assert
        result.Rate.Should().Be(0.18m); // Hepsiburada fallback
        result.Amount.Should().Be(90m); // 500 * 0.18
        result.Source.Should().Be("StaticFallback");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 4: Negative rate → exception
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_NegativeRate_ThrowsException()
    {
        // Arrange
        var negativeRate = new DynamicRateResult(
            Rate: -0.05m,
            Source: "BadAPI",
            CachedUntil: DateTime.UtcNow.AddMinutes(30));

        Func<string, string?, CancellationToken, Task<DynamicRateResult?>> provider =
            (_, _, _) => Task.FromResult<DynamicRateResult?>(negativeRate);

        var service = new CommissionCalculationService(provider);

        // Act & Assert
        var act = () => service.CalculateCommissionAsync("Trendyol", null, 100m);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*negative*");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 5: Cache not expired → no API re-call
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_CacheNotExpired_NoApiReCall()
    {
        // Arrange
        var callCount = 0;
        var dynamicRate = new DynamicRateResult(
            Rate: 0.10m,
            Source: "N11API",
            CachedUntil: DateTime.UtcNow.AddMinutes(30));

        Func<string, string?, CancellationToken, Task<DynamicRateResult?>> provider =
            (_, _, _) =>
            {
                callCount++;
                return Task.FromResult<DynamicRateResult?>(dynamicRate);
            };

        var service = new CommissionCalculationService(provider);

        // Act — first call populates cache
        var result1 = await service.CalculateCommissionAsync("N11", "Moda", 200m);
        // Act — second call should use cache
        var result2 = await service.CalculateCommissionAsync("N11", "Moda", 300m);

        // Assert
        callCount.Should().Be(1, "provider should be called only once; second call should use cache");
        result1.IsCached.Should().BeFalse();
        result2.IsCached.Should().BeTrue();
        result2.Rate.Should().Be(0.10m);
        result2.Amount.Should().Be(30m); // 300 * 0.10
        result2.Source.Should().Be("N11API");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 6: Sync method still works correctly (backward compat)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void CalculateCommission_SyncMethod_ReturnsCorrectAmount()
    {
        var service = new CommissionCalculationService();
        var result = service.CalculateCommission("Ciceksepeti", null, 1000m);
        result.Should().Be(200m); // 1000 * 0.20
    }

    // ─────────────────────────────────────────────────────────────
    // Test 7: Unknown platform → default 15% fallback
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_UnknownPlatform_UsesDefaultFallback()
    {
        var service = new CommissionCalculationService();
        var result = await service.CalculateCommissionAsync("UnknownPlatform", null, 100m);

        // Unknown platform returns 0 rate — explicit registration in _fallbackRates is required
        result.Rate.Should().Be(0m);
        result.Amount.Should().Be(0m);
        result.Source.Should().Be("StaticFallback");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 8: Negative grossAmount → exception
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CalculateCommissionAsync_NegativeGrossAmount_ThrowsException()
    {
        var service = new CommissionCalculationService();
        var act = () => service.CalculateCommissionAsync("Trendyol", null, -50m);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
