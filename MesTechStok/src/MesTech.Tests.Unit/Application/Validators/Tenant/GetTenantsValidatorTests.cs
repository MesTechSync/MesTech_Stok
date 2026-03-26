using FluentAssertions;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tenant;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetTenantsValidatorTests
{
    private readonly GetTenantsValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetTenantsQuery(1, 50);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PageZero_ShouldFail()
    {
        var query = new GetTenantsQuery(0, 50);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task NegativePage_ShouldFail()
    {
        var query = new GetTenantsQuery(-1, 50);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeZero_ShouldFail()
    {
        var query = new GetTenantsQuery(1, 0);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public async Task PageSizeExceeds200_ShouldFail()
    {
        var query = new GetTenantsQuery(1, 201);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(200)]
    public async Task ValidPageSizeBoundary_ShouldPass(int pageSize)
    {
        var query = new GetTenantsQuery(1, pageSize);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
