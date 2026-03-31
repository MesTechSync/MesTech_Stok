using FluentAssertions;
using MesTech.Application.Queries.GetSyncStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetSyncStatusValidatorTests
{
    private readonly GetSyncStatusValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetSyncStatusQuery CreateValidQuery() => new(PlatformCode: "test");
}
