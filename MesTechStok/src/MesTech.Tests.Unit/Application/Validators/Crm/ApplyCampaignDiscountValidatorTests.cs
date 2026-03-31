using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
public class ApplyCampaignDiscountValidatorTests
{
    private readonly ApplyCampaignDiscountValidator _sut = new();

    private static ApplyCampaignDiscountQuery CreateValidCommand() =>
        new(Guid.NewGuid(), 99.99m);

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task Price_WhenZero_ShouldFail()
    {
        var command = CreateValidCommand() with { Price = 0m };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Price_WhenNegative_ShouldFail()
    {
        var command = CreateValidCommand() with { Price = -10m };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Price_WhenPositive_ShouldPass()
    {
        var command = CreateValidCommand() with { Price = 0.01m };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Price_WhenLargeValue_ShouldPass()
    {
        var command = CreateValidCommand() with { Price = 999_999.99m };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenValidGuid_ShouldPass()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty, Price = -5m };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
