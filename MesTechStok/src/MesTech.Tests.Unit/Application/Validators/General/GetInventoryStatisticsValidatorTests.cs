using FluentAssertions;
using MesTech.Application.Queries.GetInventoryStatistics;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetInventoryStatisticsValidatorTests
{
    private readonly GetInventoryStatisticsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetInventoryStatisticsQuery CreateValidQuery() => new();
}
