using FluentAssertions;
using MesTech.Application.Queries.GetProductByBarcode;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetProductByBarcodeValidatorTests
{
    private readonly GetProductByBarcodeValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyBarcode_ShouldFail()
    {
        var input = CreateValidQuery() with { Barcode = "" };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public async Task BarcodeTooLong_ShouldFail()
    {
        var input = CreateValidQuery() with { Barcode = new string('X', 201) };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    private static GetProductByBarcodeQuery CreateValidQuery() => new(Barcode: "test");
}
