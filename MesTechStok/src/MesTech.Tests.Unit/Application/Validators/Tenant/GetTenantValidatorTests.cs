using FluentAssertions;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tenant;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetTenantValidatorTests
{
    private readonly GetTenantValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetTenantQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var query = new GetTenantQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
