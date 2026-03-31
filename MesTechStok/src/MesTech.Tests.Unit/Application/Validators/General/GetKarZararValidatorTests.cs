using FluentAssertions;
using MesTech.Application.Queries.GetKarZarar;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetKarZararValidatorTests
{
    private readonly GetKarZararValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetKarZararQuery CreateValidQuery() => new(From: DateTime.UtcNow.AddMonths(-1), To: DateTime.UtcNow);
}
