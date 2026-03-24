using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Erp")]
public class GetErpDashboardValidatorTests
{
    private readonly GetErpDashboardValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetErpDashboardQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetErpDashboardQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
