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

    private static GetFulfillmentInventoryQuery CreateValidQuery() => new(Center: MesTech.Application.DTOs.Fulfillment.FulfillmentCenter.OwnWarehouse, Skus: new List<string>{"SKU1"});
}
