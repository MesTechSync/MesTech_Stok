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

    private static GetProductByBarcodeQuery CreateValidQuery() => new(Barcode: "test");
}
