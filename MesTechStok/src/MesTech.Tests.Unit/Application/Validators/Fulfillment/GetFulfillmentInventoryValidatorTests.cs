using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetFulfillmentInventoryValidatorTests
{
    private readonly GetFulfillmentInventoryValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptySkus_ShouldFail()
    {
        var input = CreateValidQuery() with { Skus = new List<string>() };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Skus");
    }

    [Fact]
    public async Task InvalidCenter_ShouldFail()
    {
        var input = CreateValidQuery() with { Center = (MesTech.Application.DTOs.Fulfillment.FulfillmentCenter)999 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Center");
    }

    private static GetFulfillmentInventoryQuery CreateValidQuery() => new(Center: MesTech.Application.DTOs.Fulfillment.FulfillmentCenter.OwnWarehouse, Skus: new List<string>{"SKU1"});
}
