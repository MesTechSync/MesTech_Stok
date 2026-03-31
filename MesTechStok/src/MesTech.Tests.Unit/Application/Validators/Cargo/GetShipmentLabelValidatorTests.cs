using FluentAssertions;
using MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Cargo;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetShipmentLabelValidatorTests
{
    private readonly GetShipmentLabelValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyShipmentId_ShouldFail()
    {
        var input = CreateValidQuery() with { ShipmentId = string.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentId");
    }

    private static GetShipmentLabelQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), ShipmentId: Guid.NewGuid().ToString());
}
