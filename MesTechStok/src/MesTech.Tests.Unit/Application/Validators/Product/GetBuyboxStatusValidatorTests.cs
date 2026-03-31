using FluentAssertions;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetBuyboxStatusValidatorTests
{
    private readonly GetBuyboxStatusValidator _sut = new();

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
    public async Task EmptyProductId_ShouldFail()
    {
        var input = CreateValidQuery() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    private static GetBuyboxStatusQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), ProductId: Guid.NewGuid(), PlatformCode: "test");
}
