using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetCargoComparisonValidatorTests
{
    private readonly GetCargoComparisonValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullShipmentRequest_ShouldFail()
    {
        var input = CreateValidQuery() with { ShipmentRequest = null! };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentRequest");
    }

    private static GetCargoComparisonQuery CreateValidQuery() => new(ShipmentRequest: new MesTech.Application.DTOs.Cargo.ShipmentRequest { OrderId = Guid.NewGuid(), Weight = 1.5m, Desi = 2 });
}
