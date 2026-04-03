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

    [Fact]
    public async Task PlatformCodeTooLong_ShouldFail()
    {
        var input = CreateValidQuery() with { PlatformCode = new string('A', 51) };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    private static GetSyncStatusQuery CreateValidQuery() => new(PlatformCode: "test");
}
