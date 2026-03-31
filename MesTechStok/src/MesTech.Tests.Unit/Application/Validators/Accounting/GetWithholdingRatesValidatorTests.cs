using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetWithholdingRatesValidatorTests
{
    private readonly GetWithholdingRatesValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetWithholdingRatesQuery CreateValidQuery() => new();
}
