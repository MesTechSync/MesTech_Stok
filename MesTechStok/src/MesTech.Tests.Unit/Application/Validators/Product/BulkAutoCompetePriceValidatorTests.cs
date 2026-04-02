using FluentAssertions;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

public class BulkAutoCompetePriceValidatorTests
{
    private readonly BulkAutoCompetePriceValidator _sut = new();

    private static BulkAutoCompetePriceCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        PlatformCode: "TRENDYOL",
        FloorMarginPercent: 5m,
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
    public async Task NullPlatformCode_ShouldPass()
    {
        var command = CreateValidCommand() with { PlatformCode = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PlatformCodeTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { PlatformCode = new string('X', 51) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(51)]
    [InlineData(100)]
    public async Task FloorMarginPercent_OutOfRange_ShouldFail(decimal value)
    {
        var command = CreateValidCommand() with { FloorMarginPercent = value };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FloorMarginPercent");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public async Task FloorMarginPercent_InRange_ShouldPass(decimal value)
    {
        var command = CreateValidCommand() with { FloorMarginPercent = value };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(31)]
    [InlineData(50)]
    public async Task MaxDiscountPercent_OutOfRange_ShouldFail(decimal value)
    {
        var command = CreateValidCommand() with { MaxDiscountPercent = value };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxDiscountPercent");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0.1)]
    [InlineData(15)]
    [InlineData(30)]
    public async Task MaxDiscountPercent_InRange_ShouldPass(decimal value)
    {
        var command = CreateValidCommand() with { MaxDiscountPercent = value };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
