using FluentAssertions;
using MesTech.Application.Queries.GetStockMovements;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetStockMovementsValidatorTests
{
    private readonly GetStockMovementsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetStockMovementsQuery CreateValidQuery() => new();
}
