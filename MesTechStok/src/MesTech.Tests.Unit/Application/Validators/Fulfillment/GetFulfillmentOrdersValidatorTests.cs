using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetFulfillmentOrdersValidatorTests
{
    private readonly GetFulfillmentOrdersValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultSince_ShouldFail()
    {
        var input = CreateValidQuery() with { Since = default };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Since");
    }

    [Fact]
    public async Task InvalidCenter_ShouldFail()
    {
        var input = CreateValidQuery() with { Center = (MesTech.Application.DTOs.Fulfillment.FulfillmentCenter)999 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Center");
    }

    private static GetFulfillmentOrdersQuery CreateValidQuery() => new(Center: MesTech.Application.DTOs.Fulfillment.FulfillmentCenter.OwnWarehouse, Since: DateTime.UtcNow);
}
