using FluentAssertions;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetShipmentStatusValidatorTests
{
    private readonly GetShipmentStatusValidator _sut = new();

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

    private static GetShipmentStatusQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), TrackingNumber: "test", Provider: MesTech.Domain.Enums.CargoProvider.YurticiKargo);
}
