using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

public class CheckVknMukellefValidatorTests
{
    private readonly CheckVknMukellefValidator _sut = new();

    private static CheckVknMukellefQuery CreateValidQuery() => new(
        Vkn: "1234567890");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidVkn10Digits_ShouldPass()
    {
        var query = CreateValidQuery() with { Vkn = "1234567890" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidVkn11Digits_ShouldPass()
    {
        var query = CreateValidQuery() with { Vkn = "12345678901" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Vkn9Digits_ShouldFail()
    {
        var query = CreateValidQuery() with { Vkn = "123456789" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Vkn");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Vkn12Digits_ShouldFail()
    {
        var query = CreateValidQuery() with { Vkn = "123456789012" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Vkn");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public async Task EmptyOrNullVkn_ShouldFail(string? vkn)
    {
        var query = CreateValidQuery() with { Vkn = vkn! };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Vkn");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task VknTooShort_ShouldReturnLengthError()
    {
        var query = CreateValidQuery() with { Vkn = "12345" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Vkn");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task VknExactly10Chars_ShouldPass()
    {
        var query = CreateValidQuery() with { Vkn = "0000000000" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
