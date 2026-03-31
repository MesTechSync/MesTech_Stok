using FluentAssertions;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

public class AutoCompetePriceValidatorTests
{
    private readonly AutoCompetePriceValidator _sut = new();

    private static AutoCompetePriceCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        PlatformCode: "TRENDYOL",
        FloorPrice: 49.90m,
        MaxDiscountPercent: 5m);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyProductId_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public async Task EmptyOrNullPlatformCode_ShouldFail(string? platformCode)
    {
        var command = CreateValidCommand() with { PlatformCode = platformCode! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PlatformCodeExceeds50Chars_ShouldFail()
    {
        var command = CreateValidCommand() with { PlatformCode = new string('X', 51) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PlatformCode50Chars_ShouldPass()
    {
        var command = CreateValidCommand() with { PlatformCode = new string('X', 50) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Category", "Unit")]
    public async Task FloorPriceZeroOrNegative_ShouldFail(decimal floorPrice)
    {
        var command = CreateValidCommand() with { FloorPrice = floorPrice };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FloorPrice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30.1)]
    [Trait("Category", "Unit")]
    public async Task MaxDiscountPercentOutOfRange_ShouldFail(decimal maxDiscount)
    {
        var command = CreateValidCommand() with { MaxDiscountPercent = maxDiscount };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxDiscountPercent");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(15)]
    [InlineData(30)]
    [Trait("Category", "Unit")]
    public async Task MaxDiscountPercentWithinRange_ShouldPass(decimal maxDiscount)
    {
        var command = CreateValidCommand() with { MaxDiscountPercent = maxDiscount };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MultipleInvalidFields_ShouldReturnMultipleErrors()
    {
        var command = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            ProductId = Guid.Empty,
            PlatformCode = "",
            FloorPrice = -5m,
            MaxDiscountPercent = 50m
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(5);
    }
}
