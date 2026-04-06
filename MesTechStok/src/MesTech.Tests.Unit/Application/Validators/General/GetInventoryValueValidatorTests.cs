using FluentAssertions;
using MesTech.Application.Queries.GetInventoryValue;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetInventoryValueValidatorTests
{
    private readonly GetInventoryValueValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultQuery_ShouldProduceNoErrors()
    {
        var input = new GetInventoryValueQuery();
        var result = await _sut.ValidateAsync(input);
        result.Errors.Should().BeEmpty();
    }

    private static GetInventoryValueQuery CreateValidQuery() => new();
}
