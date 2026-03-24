using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

namespace MesTech.Tests.Unit.Application.Validators.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Feature", "Fulfillment")]
public class GetFulfillmentDashboardValidatorTests
{
    private readonly GetFulfillmentDashboardValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetFulfillmentDashboardQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetFulfillmentDashboardQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
