using FluentAssertions;
using MesTech.Application.Features.Stores.Queries.GetStoreDetail;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stores;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetStoreDetailValidatorTests
{
    private readonly GetStoreDetailValidator _sut = new();

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

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var input = CreateValidQuery() with { StoreId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    private static GetStoreDetailQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), StoreId: Guid.NewGuid());
}
