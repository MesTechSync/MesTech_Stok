using FluentAssertions;
using MesTech.Application.Commands.UpdateStockForecast;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateStockForecastValidatorTests
{
    private readonly UpdateStockForecastValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task SKU_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task SKU_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static UpdateStockForecastCommand CreateValidCommand() => new()
    {
        ProductId = Guid.NewGuid(),
        SKU = "SKU-001",
        PredictedDemand7d = 50,
        PredictedDemand14d = 100,
        PredictedDemand30d = 200,
        DaysUntilStockout = 14,
        ReorderSuggestion = 50,
        Confidence = 0.85,
        TenantId = Guid.NewGuid()
    };
}
