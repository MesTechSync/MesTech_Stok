using FluentAssertions;
using MesTech.Application.Queries.GetCariHareketler;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetCariHareketlerValidatorTests
{
    private readonly GetCariHareketlerValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCariHesapId_ShouldFail()
    {
        var input = CreateValidQuery() with { CariHesapId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CariHesapId");
    }

    private static GetCariHareketlerQuery CreateValidQuery() => new(CariHesapId: Guid.NewGuid());
}
