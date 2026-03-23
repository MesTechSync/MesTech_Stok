using FluentAssertions;
using MesTech.Application.Commands.ApplyOptimizedPrice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApplyOptimizedPriceValidatorTests
{
    private readonly ApplyOptimizedPriceValidator _sut = new();

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

    private static ApplyOptimizedPriceCommand CreateValidCommand() => new()
    {
        ProductId = Guid.NewGuid(),
        SKU = "SKU-OPT-001",
        RecommendedPrice = 99.90m,
        MinPrice = 79.90m,
        MaxPrice = 119.90m,
        Confidence = 0.92,
        TenantId = Guid.NewGuid()
    };
}
