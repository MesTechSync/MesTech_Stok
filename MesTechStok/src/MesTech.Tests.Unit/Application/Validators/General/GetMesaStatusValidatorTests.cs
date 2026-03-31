using FluentAssertions;
using MesTech.Application.Features.Health.Queries.GetMesaStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetMesaStatusValidatorTests
{
    private readonly GetMesaStatusValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetMesaStatusQuery CreateValidQuery() => new();
}
