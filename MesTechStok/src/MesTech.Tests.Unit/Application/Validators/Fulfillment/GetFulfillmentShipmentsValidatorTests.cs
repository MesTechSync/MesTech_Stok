using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;

namespace MesTech.Tests.Unit.Application.Validators.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Feature", "Fulfillment")]
public class GetFulfillmentShipmentsValidatorTests
{
    private readonly GetFulfillmentShipmentsValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetFulfillmentShipmentsQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetFulfillmentShipmentsQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageZero_Fails()
    {
        var query = new GetFulfillmentShipmentsQuery(TenantId: Guid.NewGuid(), Page: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeOver100_Fails()
    {
        var query = new GetFulfillmentShipmentsQuery(TenantId: Guid.NewGuid(), PageSize: 101);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
