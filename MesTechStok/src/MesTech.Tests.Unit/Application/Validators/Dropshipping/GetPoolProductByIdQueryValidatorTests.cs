using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetPoolProductByIdQueryValidatorTests
{
    private readonly GetPoolProductByIdQueryValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolProductId_ShouldFail()
    {
        var input = CreateValidQuery() with { PoolProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolProductId");
    }

    private static GetPoolProductByIdQuery CreateValidQuery() => new(PoolProductId: Guid.NewGuid());
}
