using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.ListFixedAssets;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
public class ListFixedAssetsValidatorTests
{
    private readonly ListFixedAssetsValidator _sut = new();

    private static ListFixedAssetsQuery CreateValidQuery() =>
        new(TenantId: Guid.NewGuid());

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task DefaultTenantId_ShouldFail()
    {
        var query = new ListFixedAssetsQuery(TenantId: default);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidTenantId_ShouldHaveNoErrors()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleCalls_ShouldBeConsistent()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result1 = await _sut.ValidateAsync(query);
        var result2 = await _sut.ValidateAsync(query);
        result1.IsValid.Should().Be(result2.IsValid);
    }
}
