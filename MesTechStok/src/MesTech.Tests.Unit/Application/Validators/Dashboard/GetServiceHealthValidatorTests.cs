using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dashboard;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetServiceHealthValidatorTests
{
    private readonly GetServiceHealthValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetServiceHealthQuery CreateValidQuery() => new();
}
